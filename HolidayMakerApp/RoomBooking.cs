using MySqlConnector;

// Sök lediga rum och boka ett av dem.
static class RoomBooking
{
    // Frågar efter sökvillkor, visar lediga rum och bokar det rum som operatören väljer.
    public static void SearchAndBook(MySqlConnection connection, long customerId)
    {
        Console.Write("Ort: ");
        string? city = Console.ReadLine()?.Trim();
        Console.Write("Incheckningsdatum (åååå-mm-dd): ");
        bool checkInOk = DateOnly.TryParse(Console.ReadLine(), out DateOnly checkIn);
        Console.Write("Utcheckningsdatum (åååå-mm-dd): ");
        bool checkOutOk = DateOnly.TryParse(Console.ReadLine(), out DateOnly checkOut);
        Console.Write("Antal personer: ");
        bool guestsOk = int.TryParse(Console.ReadLine(), out int guests);

        if (string.IsNullOrWhiteSpace(city) || !checkInOk || !checkOutOk || !guestsOk || guests < 1)
        {
            Console.WriteLine("Ogiltig inmatning. Fyll i ort, giltiga datum och minst 1 person.");
            return;
        }
        if (checkOut <= checkIn)
        {
            Console.WriteLine("Utcheckningen måste vara efter incheckningen.");
            return;
        }

        bool needPool = ConsoleInput.AskYesNo("Måste ha pool?");
        bool needRestaurant = ConsoleInput.AskYesNo("Måste ha restaurang?");
        int? maxBeach = ConsoleInput.AskOptionalNumber("Max avstånd till strand i meter (tomt = inget krav): ");
        int? maxCenter = ConsoleInput.AskOptionalNumber("Max avstånd till centrum i meter (tomt = inget krav): ");
        Console.Write("Sortera efter 1. lägst pris, 2. högst omdöme: ");
        int sortChoice = Console.ReadLine()?.Trim() == "2" ? 2 : 1;

        var search = new RoomSearch(city, checkIn, checkOut, guests, needPool, needRestaurant, maxBeach, maxCenter, sortChoice);
        var rooms = FindRooms(connection, search);

        if (rooms.Count == 0)
        {
            Console.WriteLine("Inga lediga rum hittades.");
            return;
        }

        Console.Write("\nAnge numret på rummet du vill boka (tomt = tillbaka): ");
        string? roomInput = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(roomInput))
        {
            return;
        }
        if (!int.TryParse(roomInput, out int roomNr) || roomNr < 1 || roomNr > rooms.Count)
        {
            Console.WriteLine("Ogiltigt val.");
            return;
        }

        BookRoom(connection, customerId, search, rooms[roomNr - 1]);
    }

    // Söker lediga rum och skriver ut dem. SQL sköter filtrering, tillgänglighet och sortering.
    static List<(long Id, decimal Price)> FindRooms(MySqlConnection connection, RoomSearch search)
    {
        const string searchRoomsSql =
        """
        SELECT boende.namn, boende.ort, rum.id AS rum_id, rum.rumsnummer,
               rum.max_antal_personer, rum.pris_per_natt, boende.omdome
        FROM rum
        JOIN boende ON boende.id = rum.boende_id
        WHERE boende.ort = @ort
            AND rum.max_antal_personer >= @antal_pers
            AND (@kraver_pool = 0 OR boende.pool = 1)
            AND (@kraver_restaurang = 0 OR boende.restaurang = 1)
            AND (@max_strand IS NULL OR boende.avstand_till_strand_m <= @max_strand)
            AND (@max_centrum IS NULL OR boende.avstand_till_centrum_m <= @max_centrum)
            AND NOT EXISTS (
                SELECT 1
                FROM bokning
                WHERE bokning.rum_id = rum.id
                    AND bokning.incheckningsdatum < @ut_check
                    AND bokning.utcheckningsdatum > @in_check
            )
        ORDER BY CASE WHEN @sortering = 2 THEN boende.omdome END DESC,
                 rum.pris_per_natt ASC;
        """;

        var rooms = new List<(long Id, decimal Price)>();
        using var command = new MySqlCommand(searchRoomsSql, connection);
        command.Parameters.AddWithValue("@ort", search.City);
        command.Parameters.AddWithValue("@antal_pers", search.Guests);
        command.Parameters.AddWithValue("@kraver_pool", search.NeedPool ? 1 : 0);
        command.Parameters.AddWithValue("@kraver_restaurang", search.NeedRestaurant ? 1 : 0);
        command.Parameters.AddWithValue("@max_strand", (object?)search.MaxBeach ?? DBNull.Value);
        command.Parameters.AddWithValue("@max_centrum", (object?)search.MaxCenter ?? DBNull.Value);
        command.Parameters.AddWithValue("@in_check", search.CheckIn.ToDateTime(TimeOnly.MinValue));
        command.Parameters.AddWithValue("@ut_check", search.CheckOut.ToDateTime(TimeOnly.MinValue));
        command.Parameters.AddWithValue("@sortering", search.SortChoice);

        using var reader = command.ExecuteReader();
        Console.WriteLine();
        while (reader.Read())
        {
            rooms.Add((reader.GetInt64("rum_id"), Convert.ToDecimal(reader["pris_per_natt"])));
            Console.WriteLine(
                $"{rooms.Count}. {reader["namn"],-20} {reader["ort"],-12} " +
                $"rum {reader["rumsnummer"],-5} {reader["max_antal_personer"]} pers  " +
                $"{reader["pris_per_natt"]} kr/natt  omdöme {reader["omdome"]}");
        }
        return rooms;
    }

    // Kontrollerar tillgängligheten igen, sparar bokningen och visar en bekräftelse.
    static void BookRoom(MySqlConnection connection, long customerId, RoomSearch search, (long Id, decimal Price) room)
    {
        DateTime checkInDate = search.CheckIn.ToDateTime(TimeOnly.MinValue);
        DateTime checkOutDate = search.CheckOut.ToDateTime(TimeOnly.MinValue);

        const string checkAvailabilitySql =
        """
        SELECT COUNT(*)
        FROM rum
        WHERE rum.id = @rum_id
            AND rum.max_antal_personer >= @antal_pers
            AND NOT EXISTS (
                SELECT 1
                FROM bokning
                WHERE bokning.rum_id = rum.id
                    AND bokning.incheckningsdatum < @ut_check
                    AND bokning.utcheckningsdatum > @in_check
            );
        """;
        using var checkCommand = new MySqlCommand(checkAvailabilitySql, connection);
        checkCommand.Parameters.AddWithValue("@rum_id", room.Id);
        checkCommand.Parameters.AddWithValue("@antal_pers", search.Guests);
        checkCommand.Parameters.AddWithValue("@in_check", checkInDate);
        checkCommand.Parameters.AddWithValue("@ut_check", checkOutDate);

        if (Convert.ToInt64(checkCommand.ExecuteScalar()) == 0)
        {
            Console.WriteLine("Rummet är inte längre ledigt för den perioden.");
            return;
        }

        const string insertBookingSql =
        """
        INSERT INTO bokning (kund_id, rum_id, incheckningsdatum, utcheckningsdatum, antal_personer)
        VALUES (@kund_id, @rum_id, @in_check, @ut_check, @antal_pers);
        """;
        using var insertCommand = new MySqlCommand(insertBookingSql, connection);
        insertCommand.Parameters.AddWithValue("@kund_id", customerId);
        insertCommand.Parameters.AddWithValue("@rum_id", room.Id);
        insertCommand.Parameters.AddWithValue("@in_check", checkInDate);
        insertCommand.Parameters.AddWithValue("@ut_check", checkOutDate);
        insertCommand.Parameters.AddWithValue("@antal_pers", search.Guests);
        insertCommand.ExecuteNonQuery();
        long bookingId = insertCommand.LastInsertedId;

        int nights = search.CheckOut.DayNumber - search.CheckIn.DayNumber;
        decimal totalPrice = nights * room.Price;
        Console.WriteLine("\n=== Bokningsbekräftelse ===");
        Console.WriteLine($"Bokningsnummer: {bookingId}");
        Console.WriteLine($"{search.CheckIn:yyyy-MM-dd} till {search.CheckOut:yyyy-MM-dd} ({nights} nätter), {search.Guests} personer");
        Console.WriteLine($"Totalpris: {totalPrice} kr");
    }
}

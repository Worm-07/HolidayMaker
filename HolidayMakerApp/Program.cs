using MySqlConnector;

const string connectionString =
    "Server = localhost; Port = 3306; Database = public; User ID = root; Password = kurslosenord";
using var connection = new MySqlConnection(connectionString);
connection.Open();

bool running = true;
long? customerId = null;


while (running)
{
    try
    {
        Console.WriteLine("\n=== Meny ===");
        Console.WriteLine("1. Välj en befintlig kund");
        Console.WriteLine("2. Registrera en ny kund");
        Console.WriteLine("3. Avsluta");

        string? input = Console.ReadLine();
        if (int.TryParse(input, out int choice))
        {
            switch (choice)
            {
                case 1:
                    {
                        const string customerListSql =
    """
SELECT id, namn, email, telefonnummer
FROM kund
ORDER BY namn
""";
                        using var command = new MySqlCommand(customerListSql, connection);
                        using var reader = command.ExecuteReader();
                        var customer = new List<long>();
                        var customerNames = new List<string>();

                        int i = 1;
                        while (reader.Read())
                        {
                            long id = reader.GetInt64("id");
                            string name = reader.GetString("namn");
                            string email = reader.GetString("email");

                            Console.WriteLine($"{i}. {name,-20} {email}");
                            i++;
                            customer.Add(id);
                            customerNames.Add(name);
                        }
                        reader.Close();

                        Console.WriteLine("\n1. Välj en kund");
                        Console.WriteLine("2. Tillbaka");

                        string? input1 = Console.ReadLine();
                        if (int.TryParse(input1, out int choice1))
                        {
                            if (choice1 == 1)
                            {
                                if
                                (!int.TryParse(Console.ReadLine(), out int chosenCustomer)
                                    || chosenCustomer < 1
                                    || chosenCustomer > customer.Count)
                                {
                                    Console.WriteLine("Ogiltigt kundval.");
                                }
                                else
                                {
                                    customerId = customer[chosenCustomer - 1];
                                    string selectedCustomerName = customerNames[chosenCustomer - 1];
                                    Console.WriteLine($"Vald kund: {selectedCustomerName}");
                                }
                            }
                        }
                        while (customerId != null)
                        {
                            Console.WriteLine("\n1. Sök lediga rum och boka");
                            Console.WriteLine("2. Visa kundens bokningar");
                            Console.WriteLine("3. Ändra antal personer");
                            Console.WriteLine("4. Avboka");
                            Console.WriteLine("5. Tillbaka");

                            string? input2 = Console.ReadLine();
                            if (int.TryParse(input2, out int output2))
                            {
                                if (output2 == 1)
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
                                    }
                                    else if (checkOut <= checkIn)
                                    {
                                        Console.WriteLine("Utcheckningen måste vara efter incheckningen.");
                                    }
                                    else
                                    {
                                        bool needPool = AskYesNo("Måste ha pool?");
                                        bool needRestaurant = AskYesNo("Måste ha restaurang?");
                                        int? maxBeach = AskOptionalNumber("Max avstånd till strand i meter (tomt = inget krav): ");
                                        int? maxCenter = AskOptionalNumber("Max avstånd till centrum i meter (tomt = inget krav): ");
                                        Console.Write("Sortera efter 1. lägst pris, 2. högst omdöme: ");
                                        int sortChoice = Console.ReadLine()?.Trim() == "2" ? 2 : 1;

                                        DateTime checkInDate = checkIn.ToDateTime(TimeOnly.MinValue);
                                        DateTime checkOutDate = checkOut.ToDateTime(TimeOnly.MinValue);

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
                                        using (var searchCommand = new MySqlCommand(searchRoomsSql, connection))
                                        {
                                            searchCommand.Parameters.AddWithValue("@ort", city);
                                            searchCommand.Parameters.AddWithValue("@antal_pers", guests);
                                            searchCommand.Parameters.AddWithValue("@kraver_pool", needPool ? 1 : 0);
                                            searchCommand.Parameters.AddWithValue("@kraver_restaurang", needRestaurant ? 1 : 0);
                                            searchCommand.Parameters.AddWithValue("@max_strand", (object?)maxBeach ?? DBNull.Value);
                                            searchCommand.Parameters.AddWithValue("@max_centrum", (object?)maxCenter ?? DBNull.Value);
                                            searchCommand.Parameters.AddWithValue("@in_check", checkInDate);
                                            searchCommand.Parameters.AddWithValue("@ut_check", checkOutDate);
                                            searchCommand.Parameters.AddWithValue("@sortering", sortChoice);

                                            using var searchReader = searchCommand.ExecuteReader();
                                            Console.WriteLine();
                                            while (searchReader.Read())
                                            {
                                                rooms.Add((searchReader.GetInt64("rum_id"), Convert.ToDecimal(searchReader["pris_per_natt"])));
                                                Console.WriteLine(
                                                    $"{rooms.Count}. {searchReader["namn"],-20} {searchReader["ort"],-12} " +
                                                    $"rum {searchReader["rumsnummer"],-5} {searchReader["max_antal_personer"]} pers  " +
                                                    $"{searchReader["pris_per_natt"]} kr/natt  omdöme {searchReader["omdome"]}");
                                            }
                                        }

                                        if (rooms.Count == 0)
                                        {
                                            Console.WriteLine("Inga lediga rum hittades.");
                                        }
                                        else
                                        {
                                            Console.Write("\nAnge numret på rummet du vill boka (tomt = tillbaka): ");
                                            string? roomInput = Console.ReadLine();
                                            if (!string.IsNullOrWhiteSpace(roomInput))
                                            {
                                                if (!int.TryParse(roomInput, out int roomNr) || roomNr < 1 || roomNr > rooms.Count)
                                                {
                                                    Console.WriteLine("Ogiltigt val.");
                                                }
                                                else
                                                {
                                                    long roomId = rooms[roomNr - 1].Id;

                                                    // Kontrollerar tillgängligheten igen precis innan bokningen sparas
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
                                                    checkCommand.Parameters.AddWithValue("@rum_id", roomId);
                                                    checkCommand.Parameters.AddWithValue("@antal_pers", guests);
                                                    checkCommand.Parameters.AddWithValue("@in_check", checkInDate);
                                                    checkCommand.Parameters.AddWithValue("@ut_check", checkOutDate);

                                                    if (Convert.ToInt64(checkCommand.ExecuteScalar()) == 0)
                                                    {
                                                        Console.WriteLine("Rummet är inte längre ledigt för den perioden.");
                                                    }
                                                    else
                                                    {
                                                        const string insertBookingSql =
                                                        """
                                                        INSERT INTO bokning (kund_id, rum_id, incheckningsdatum, utcheckningsdatum, antal_personer)
                                                        VALUES (@kund_id, @rum_id, @in_check, @ut_check, @antal_pers);
                                                        """;
                                                        using var insertCommand = new MySqlCommand(insertBookingSql, connection);
                                                        insertCommand.Parameters.AddWithValue("@kund_id", customerId);
                                                        insertCommand.Parameters.AddWithValue("@rum_id", roomId);
                                                        insertCommand.Parameters.AddWithValue("@in_check", checkInDate);
                                                        insertCommand.Parameters.AddWithValue("@ut_check", checkOutDate);
                                                        insertCommand.Parameters.AddWithValue("@antal_pers", guests);
                                                        insertCommand.ExecuteNonQuery();
                                                        long bookingId = insertCommand.LastInsertedId;

                                                        int nights = checkOut.DayNumber - checkIn.DayNumber;
                                                        decimal totalPrice = nights * rooms[roomNr - 1].Price;
                                                        Console.WriteLine("\n=== Bokningsbekräftelse ===");
                                                        Console.WriteLine($"Bokningsnummer: {bookingId}");
                                                        Console.WriteLine($"{checkIn:yyyy-MM-dd} till {checkOut:yyyy-MM-dd} ({nights} nätter), {guests} personer");
                                                        Console.WriteLine($"Totalpris: {totalPrice} kr");
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                else if (output2 == 2)
                                {
                                    const string customerBookingsSql =
                                    """
                                    SELECT bokning.id AS bokningsnummer,
                                    boende.namn AS boende,
                                    boende.ort AS ort,
                                    rum.rumsnummer,
                                    bokning.incheckningsdatum,
                                    bokning.utcheckningsdatum,
                                    bokning.antal_personer,
                                    DATEDIFF(bokning.utcheckningsdatum, bokning.incheckningsdatum) AS antal_natter,
                                    DATEDIFF(bokning.utcheckningsdatum, bokning.incheckningsdatum) * rum.pris_per_natt AS totalpris
                                    FROM bokning
                                    JOIN rum ON rum.id = bokning.rum_id
                                    JOIN boende ON boende.id = rum.boende_id
                                    WHERE bokning.kund_id = @kund_id
                                    ORDER BY bokning.incheckningsdatum;
                                    """;
                                    using var bookingsCommand = new MySqlCommand(customerBookingsSql, connection);
                                    bookingsCommand.Parameters.AddWithValue("@kund_id", customerId);
                                    using var bookingsReader = bookingsCommand.ExecuteReader();

                                    bool hasBookings = false;
                                    while (bookingsReader.Read())
                                    {
                                        hasBookings = true;
                                        Console.WriteLine(
                                            $"Bokning {bookingsReader["bokningsnummer"]}: " +
                                            $"{bookingsReader["boende"]}, {bookingsReader["ort"]} - rum {bookingsReader["rumsnummer"]}");
                                        Console.WriteLine(
                                            $"   {bookingsReader.GetDateTime("incheckningsdatum"):yyyy-MM-dd} till " +
                                            $"{bookingsReader.GetDateTime("utcheckningsdatum"):yyyy-MM-dd} " +
                                            $"({bookingsReader["antal_natter"]} nätter), " +
                                            $"{bookingsReader["antal_personer"]} personer, totalpris {bookingsReader["totalpris"]} kr");
                                    }
                                    if (!hasBookings)
                                    {
                                        Console.WriteLine("Kunden har inga bokningar.");
                                    }
                                }
                                else if (output2 == 5)
                                {
                                    customerId = null;
                                }
                                else if (output2 == 3)
                                {
                                    var changeList = ShowBookings(connection, customerId.Value);
                                    if (changeList.Count > 0)
                                    {
                                        Console.Write("\nVälj bokning att ändra (tomt = tillbaka): ");
                                        string? changeInput = Console.ReadLine();
                                        if (!string.IsNullOrWhiteSpace(changeInput))
                                        {
                                            if (!int.TryParse(changeInput, out int changeNr) || changeNr < 1 || changeNr > changeList.Count)
                                            {
                                                Console.WriteLine("Ogiltigt val.");
                                            }
                                            else
                                            {
                                                var chosenBooking = changeList[changeNr - 1];
                                                Console.Write("Nytt antal personer: ");
                                                if (!int.TryParse(Console.ReadLine(), out int newGuests) || newGuests < 1)
                                                {
                                                    Console.WriteLine("Ogiltigt antal personer.");
                                                }
                                                else if (newGuests > chosenBooking.Capacity)
                                                {
                                                    Console.WriteLine($"Rummet rymmer högst {chosenBooking.Capacity} personer.");
                                                }
                                                else
                                                {
                                                    // Kapaciteten kontrolleras även i SQL så att inget ogiltigt kan sparas
                                                    const string updateGuestsSql =
                                                    """
                                                    UPDATE bokning
                                                    JOIN rum ON rum.id = bokning.rum_id
                                                    SET bokning.antal_personer = @nytt_antal
                                                    WHERE bokning.id = @bokning_id
                                                        AND bokning.kund_id = @kund_id
                                                        AND rum.max_antal_personer >= @nytt_antal;
                                                    """;
                                                    using var updateCommand = new MySqlCommand(updateGuestsSql, connection);
                                                    updateCommand.Parameters.AddWithValue("@nytt_antal", newGuests);
                                                    updateCommand.Parameters.AddWithValue("@bokning_id", chosenBooking.Id);
                                                    updateCommand.Parameters.AddWithValue("@kund_id", customerId);

                                                    if (updateCommand.ExecuteNonQuery() > 0)
                                                    {
                                                        Console.WriteLine("Antal personer ändrat.");
                                                    }
                                                    else
                                                    {
                                                        Console.WriteLine("Ändringen kunde inte genomföras.");
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                else if (output2 == 4)
                                {
                                    var cancelList = ShowBookings(connection, customerId.Value);
                                    if (cancelList.Count > 0)
                                    {
                                        Console.Write("\nVälj bokning att avboka (tomt = tillbaka): ");
                                        string? cancelInput = Console.ReadLine();
                                        if (!string.IsNullOrWhiteSpace(cancelInput))
                                        {
                                            if (!int.TryParse(cancelInput, out int cancelNr) || cancelNr < 1 || cancelNr > cancelList.Count)
                                            {
                                                Console.WriteLine("Ogiltigt val.");
                                            }
                                            else if (AskYesNo("Är du säker på att du vill avboka?"))
                                            {
                                                const string deleteBookingSql =
                                                """
                                                DELETE FROM bokning
                                                WHERE id = @bokning_id
                                                    AND kund_id = @kund_id;
                                                """;
                                                using var deleteCommand = new MySqlCommand(deleteBookingSql, connection);
                                                deleteCommand.Parameters.AddWithValue("@bokning_id", cancelList[cancelNr - 1].Id);
                                                deleteCommand.Parameters.AddWithValue("@kund_id", customerId);

                                                if (deleteCommand.ExecuteNonQuery() > 0)
                                                {
                                                    Console.WriteLine("Bokningen är avbokad.");
                                                }
                                                else
                                                {
                                                    Console.WriteLine("Bokningen kunde inte avbokas.");
                                                }
                                            }
                                            else
                                            {
                                                Console.WriteLine("Avbokningen avbröts.");
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("Välj ett menyval!");
                                }
                            }
                            else
                            {
                                Console.WriteLine("Välj ett menyval!");
                            }
                        }
                        break;
                    }
                case 2:
                    {
                        Console.Write("Ange namn: ");
                        string? name = Console.ReadLine();
                        Console.Write("Ange email: ");
                        string? email = Console.ReadLine();
                        Console.Write("Ange telefonnummer: ");
                        string? telephoneNumber = Console.ReadLine();

                        if (
                            string.IsNullOrWhiteSpace(name)
                            || string.IsNullOrWhiteSpace(email)
                            || string.IsNullOrWhiteSpace(telephoneNumber)
                            )
                        {
                            Console.WriteLine("Du måste skriva in ett giltigt namn, email, telefonnummer!");
                        }
                        else
                        {
                            const string sql =
    """
INSERT INTO kund (namn, email, telefonnummer) 
VALUES (@namn, @email, @telefon)
""";
                            using var command = new MySqlCommand(sql, connection);
                            command.Parameters.AddWithValue("@namn", name);
                            command.Parameters.AddWithValue("@email", email);
                            command.Parameters.AddWithValue("@telefon", telephoneNumber);

                            command.ExecuteNonQuery();
                            Console.WriteLine("Kund registrerad.");
                        }
                        break;
                    }
                case 3:
                    running = false;
                    break;
                default:
                    Console.WriteLine("=== Välj ett menyval! ===");
                    break;
            }
        }
        else
        {
            Console.WriteLine("=== Välj ett menyval! ===");
        }
    }
    catch (MySqlException ex)
    {
        Console.WriteLine($"Databasfel: {ex.Message}");
    }
}

// Visar kundens bokningar numrerade och returnerar dem så att operatören kan välja en.
static List<(long Id, int Guests, int Capacity)> ShowBookings(MySqlConnection connection, long customerId)
{
    const string sql =
    """
    SELECT bokning.id AS bokningsnummer,
           boende.namn AS boende,
           rum.rumsnummer,
           bokning.incheckningsdatum,
           bokning.utcheckningsdatum,
           bokning.antal_personer,
           rum.max_antal_personer
    FROM bokning
    JOIN rum ON rum.id = bokning.rum_id
    JOIN boende ON boende.id = rum.boende_id
    WHERE bokning.kund_id = @kund_id
    ORDER BY bokning.incheckningsdatum;
    """;

    var bookings = new List<(long Id, int Guests, int Capacity)>();
    using var command = new MySqlCommand(sql, connection);
    command.Parameters.AddWithValue("@kund_id", customerId);
    using var reader = command.ExecuteReader();

    Console.WriteLine();
    while (reader.Read())
    {
        int guests = Convert.ToInt32(reader["antal_personer"]);
        int capacity = Convert.ToInt32(reader["max_antal_personer"]);
        bookings.Add((reader.GetInt64("bokningsnummer"), guests, capacity));
        Console.WriteLine(
            $"{bookings.Count}. Bokning {reader["bokningsnummer"]}: {reader["boende"]}, rum {reader["rumsnummer"]}, " +
            $"{reader.GetDateTime("incheckningsdatum"):yyyy-MM-dd} till {reader.GetDateTime("utcheckningsdatum"):yyyy-MM-dd}, " +
            $"{guests} av max {capacity} personer");
    }

    if (bookings.Count == 0)
    {
        Console.WriteLine("Kunden har inga bokningar.");
    }
    return bookings;
}

// Ställer en ja/nej-fråga. Allt utom "j" räknas som nej.
static bool AskYesNo(string question)
{
    Console.Write($"{question} (j/n): ");
    return Console.ReadLine()?.Trim().ToLower() == "j";
}

// Läser ett valfritt heltal. Tomt svar ger null, ogiltigt svar frågar igen.
static int? AskOptionalNumber(string question)
{
    while (true)
    {
        Console.Write(question);
        string? answer = Console.ReadLine()?.Trim();
        if (string.IsNullOrEmpty(answer))
        {
            return null;
        }
        if (int.TryParse(answer, out int number) && number >= 0)
        {
            return number;
        }
        Console.WriteLine("Skriv ett heltal, eller lämna tomt.");
    }
}
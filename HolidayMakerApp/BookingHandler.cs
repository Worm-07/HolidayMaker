using MySqlConnector;

// Befintliga bokningar: visa, ändra antal personer och avboka.
static class BookingHandler
{
    // Visar kundens bokningar med boende, rum, datum, antal personer och totalpris.
    public static void ShowBookingDetails(MySqlConnection connection, long customerId)
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
        using var command = new MySqlCommand(customerBookingsSql, connection);
        command.Parameters.AddWithValue("@kund_id", customerId);
        using var reader = command.ExecuteReader();

        bool hasBookings = false;
        while (reader.Read())
        {
            hasBookings = true;
            Console.WriteLine(
                $"Bokning {reader["bokningsnummer"]}: " +
                $"{reader["boende"]}, {reader["ort"]} - rum {reader["rumsnummer"]}");
            Console.WriteLine(
                $"   {reader.GetDateTime("incheckningsdatum"):yyyy-MM-dd} till " +
                $"{reader.GetDateTime("utcheckningsdatum"):yyyy-MM-dd} " +
                $"({reader["antal_natter"]} nätter), " +
                $"{reader["antal_personer"]} personer, totalpris {reader["totalpris"]} kr");
        }
        if (!hasBookings)
        {
            Console.WriteLine("Kunden har inga bokningar.");
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

    // Ändrar antal personer i en bokning, men bara om det ryms i rummet.
    public static void ChangeGuests(MySqlConnection connection, long customerId)
    {
        var bookings = ShowBookings(connection, customerId);
        if (bookings.Count == 0)
        {
            return;
        }

        Console.Write("\nVälj bokning att ändra (tomt = tillbaka): ");
        string? input = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(input))
        {
            return;
        }
        if (!int.TryParse(input, out int nr) || nr < 1 || nr > bookings.Count)
        {
            Console.WriteLine("Ogiltigt val.");
            return;
        }

        var booking = bookings[nr - 1];
        Console.Write("Nytt antal personer: ");
        if (!int.TryParse(Console.ReadLine(), out int newGuests) || newGuests < 1)
        {
            Console.WriteLine("Ogiltigt antal personer.");
            return;
        }
        if (newGuests > booking.Capacity)
        {
            Console.WriteLine($"Rummet rymmer högst {booking.Capacity} personer.");
            return;
        }

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
        using var command = new MySqlCommand(updateGuestsSql, connection);
        command.Parameters.AddWithValue("@nytt_antal", newGuests);
        command.Parameters.AddWithValue("@bokning_id", booking.Id);
        command.Parameters.AddWithValue("@kund_id", customerId);

        Console.WriteLine(command.ExecuteNonQuery() > 0
            ? "Antal personer ändrat."
            : "Ändringen kunde inte genomföras.");
    }

    // Tar bort en vald bokning efter bekräftelse. Rummet blir då ledigt igen.
    public static void CancelBooking(MySqlConnection connection, long customerId)
    {
        var bookings = ShowBookings(connection, customerId);
        if (bookings.Count == 0)
        {
            return;
        }

        Console.Write("\nVälj bokning att avboka (tomt = tillbaka): ");
        string? input = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(input))
        {
            return;
        }
        if (!int.TryParse(input, out int nr) || nr < 1 || nr > bookings.Count)
        {
            Console.WriteLine("Ogiltigt val.");
            return;
        }
        if (!ConsoleInput.AskYesNo("Är du säker på att du vill avboka?"))
        {
            Console.WriteLine("Avbokningen avbröts.");
            return;
        }

        const string deleteBookingSql =
        """
        DELETE FROM bokning
        WHERE id = @bokning_id
            AND kund_id = @kund_id;
        """;
        using var command = new MySqlCommand(deleteBookingSql, connection);
        command.Parameters.AddWithValue("@bokning_id", bookings[nr - 1].Id);
        command.Parameters.AddWithValue("@kund_id", customerId);

        Console.WriteLine(command.ExecuteNonQuery() > 0
            ? "Bokningen är avbokad."
            : "Bokningen kunde inte avbokas.");
    }
}

using MySqlConnector;

// Allt som rör kunder: välja, registrera och kundmenyn.
static class CustomerHandler
{
    // Listar kunderna, låter operatören välja en och öppnar sedan kundmenyn.
    public static void ChooseCustomer(MySqlConnection connection)
    {
        const string customerListSql =
        """
        SELECT id, namn, email, telefonnummer
        FROM kund
        ORDER BY namn
        """;

        var customerIds = new List<long>();
        var customerNames = new List<string>();
        using (var command = new MySqlCommand(customerListSql, connection))
        using (var reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                string name = reader.GetString("namn");
                string email = reader.GetString("email");
                customerIds.Add(reader.GetInt64("id"));
                customerNames.Add(name);
                Console.WriteLine($"{customerIds.Count}. {name,-20} {email}");
            }
        }

        if (customerIds.Count == 0)
        {
            Console.WriteLine("Det finns inga kunder ännu.");
            return;
        }

        Console.WriteLine("\n1. Välj en kund");
        Console.WriteLine("2. Tillbaka");
        if (!int.TryParse(Console.ReadLine(), out int menuChoice) || menuChoice != 1)
        {
            return;
        }

        if (!int.TryParse(Console.ReadLine(), out int chosenCustomer)
            || chosenCustomer < 1
            || chosenCustomer > customerIds.Count)
        {
            Console.WriteLine("Ogiltigt kundval.");
            return;
        }

        Console.WriteLine($"Vald kund: {customerNames[chosenCustomer - 1]}");
        CustomerMenu(connection, customerIds[chosenCustomer - 1]);
    }

    public static void RegisterCustomer(MySqlConnection connection)
    {
        Console.Write("Ange namn: ");
        string? name = Console.ReadLine();
        Console.Write("Ange email: ");
        string? email = Console.ReadLine();
        Console.Write("Ange telefonnummer: ");
        string? telephoneNumber = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(name)
            || string.IsNullOrWhiteSpace(email)
            || string.IsNullOrWhiteSpace(telephoneNumber))
        {
            Console.WriteLine("Du måste skriva in ett giltigt namn, email, telefonnummer!");
            return;
        }

        const string insertCustomerSql =
        """
        INSERT INTO kund (namn, email, telefonnummer)
        VALUES (@namn, @email, @telefon)
        """;
        using var command = new MySqlCommand(insertCustomerSql, connection);
        command.Parameters.AddWithValue("@namn", name);
        command.Parameters.AddWithValue("@email", email);
        command.Parameters.AddWithValue("@telefon", telephoneNumber);
        command.ExecuteNonQuery();
        Console.WriteLine("Kund registrerad.");
    }

    // Menyn som visas när en kund är vald. Körs tills operatören väljer "Tillbaka".
    static void CustomerMenu(MySqlConnection connection, long customerId)
    {
        while (true)
        {
            try
            {
                Console.WriteLine("\n1. Sök lediga rum och boka");
                Console.WriteLine("2. Visa kundens bokningar");
                Console.WriteLine("3. Ändra antal personer");
                Console.WriteLine("4. Avboka");
                Console.WriteLine("5. Tillbaka");

                if (!int.TryParse(Console.ReadLine(), out int choice))
                {
                    Console.WriteLine("Välj ett menyval!");
                    continue;
                }

                switch (choice)
                {
                    case 1:
                        RoomBooking.SearchAndBook(connection, customerId);
                        break;
                    case 2:
                        BookingHandler.ShowBookingDetails(connection, customerId);
                        break;
                    case 3:
                        BookingHandler.ChangeGuests(connection, customerId);
                        break;
                    case 4:
                        BookingHandler.CancelBooking(connection, customerId);
                        break;
                    case 5:
                        return;
                    default:
                        Console.WriteLine("Välj ett menyval!");
                        break;
                }
            }
            catch (MySqlException ex)
            {
                Console.WriteLine($"Databasfel: {ex.Message}");
            }
        }
    }
}

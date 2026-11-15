using MySqlConnector;

const string connectionString =
    "Server = localhost; Port = 3306; Database = public; User ID = root; Password = kurslosenord";
using var connection = new MySqlConnection(connectionString);
connection.Open();

bool running = true;
while (running)
{
    Console.WriteLine("=== Meny ===");
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
                    const string sql =
"""
SELECT id, namn, email, telefonnummer
FROM kund
ORDER BY namn
""";
                    using var command = new MySqlCommand(sql, connection);
                    using var reader = command.ExecuteReader();

                    int i = 1;
                    while (reader.Read())
                    {
                        long id = reader.GetInt64("id");
                        string namn = reader.GetString("namn");
                        string email = reader.GetString("email");

                        Console.WriteLine($"{i}. {namn,-20} {email}");
                        i++;
                    }
                    break;
                }
            case 2:
                {
                    Console.WriteLine("Ange namn: ");
                    string? namn = Console.ReadLine();
                    Console.WriteLine("Ange email: ");
                    string? email = Console.ReadLine();
                    Console.WriteLine("Ange telefonnummer: ");
                    string? telefonnummer = Console.ReadLine();

                    if (
                        string.IsNullOrWhiteSpace(namn)
                        || string.IsNullOrWhiteSpace(email)
                        || string.IsNullOrWhiteSpace(telefonnummer)
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
                        command.Parameters.AddWithValue("@namn", namn);
                        command.Parameters.AddWithValue("@email", email);
                        command.Parameters.AddWithValue("@telefon", telefonnummer);

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
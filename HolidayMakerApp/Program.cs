using MySqlConnector;

const string connectionString =
    "Server = localhost; Port = 3306; Database = public; User ID = root; Password = kurslosenord";
using var connection = new MySqlConnection(connectionString);
connection.Open();

Console.WriteLine("=== Meny ===");
Console.WriteLine("1. Välj en befintlig kund");
Console.WriteLine("2. Registrera en ny kund");
Console.WriteLine("3. Avsluta");

string? input = Console.ReadLine();
if (int.TryParse(input, out int choice))
{
    if (choice >= 1 && choice <= 3)
    {
        if (choice == 1)
        {
            const string sql = """
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
        }
    }
}
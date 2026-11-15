using MySqlConnector;

const string connectionString =
    "Server = localhost; Port = 3306; Database = public; User ID = root; Password = kurslosenord";
using var connection = new MySqlConnection(connectionString);
connection.Open();

bool running = true;
while (running)
{
    try
    {
        Console.WriteLine("\n=== Meny ===");
        Console.WriteLine("1. Välj en befintlig kund");
        Console.WriteLine("2. Registrera en ny kund");
        Console.WriteLine("3. Avsluta");

        if (!int.TryParse(Console.ReadLine(), out int choice))
        {
            Console.WriteLine("=== Välj ett menyval! ===");
            continue;
        }

        switch (choice)
        {
            case 1:
                CustomerHandler.ChooseCustomer(connection);
                break;
            case 2:
                CustomerHandler.RegisterCustomer(connection);
                break;
            case 3:
                running = false;
                break;
            default:
                Console.WriteLine("=== Välj ett menyval! ===");
                break;
        }
    }
    catch (MySqlException ex)
    {
        Console.WriteLine($"Databasfel: {ex.Message}");
    }
}

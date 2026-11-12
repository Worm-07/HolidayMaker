using MySqlConnector;

const string connectionString =
    "Server = localhost; Port = 3306; Database = public; User ID = root; Password = kurslosenord";

using var connection = new MySqlConnection(connectionString);
connection.Open();

const string sql = """
SELECT id, namn, email, telefonnummer
FROM kund
ORDER BY id
""";
using var command = new MySqlCommand(sql, connection);
using var reader = command.ExecuteReader();

while (reader.Read())
{
    long id = reader.GetInt64("id");
    string namn = reader.GetString("namn");
    string email = reader.GetString("email");

    Console.WriteLine($"{id,3} {namn,-20} {email}");
}
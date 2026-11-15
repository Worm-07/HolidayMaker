// Små hjälpmetoder för att läsa inmatning från konsolen.
static class ConsoleInput
{
    // Ställer en ja/nej-fråga. Allt utom "j" räknas som nej.
    public static bool AskYesNo(string question)
    {
        Console.Write($"{question} (j/n): ");
        return Console.ReadLine()?.Trim().ToLower() == "j";
    }

    // Läser ett valfritt heltal. Tomt svar ger null, ogiltigt svar frågar igen.
    public static int? AskOptionalNumber(string question)
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
}

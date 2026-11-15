// Samlar sökvillkoren på ett ställe så att de kan skickas som en enda parameter.
record RoomSearch(
    string City,
    DateOnly CheckIn,
    DateOnly CheckOut,
    int Guests,
    bool NeedPool,
    bool NeedRestaurant,
    int? MaxBeach,
    int? MaxCenter,
    int SortChoice);

-- Visa bokningen innan den tas bort
SET @kund_id = NULL, -- Lämnar tom för att inte råka ta bort något
    @bokning_id = NULL; -- Lämnar tom för att inte råka ta bort något

SELECT bokning.id AS bokningsnummer,
       boende.namn AS boende,
       rum.rumsnummer,
       bokning.incheckningsdatum,
       bokning.utcheckningsdatum
FROM bokning
JOIN rum ON rum.id = bokning.rum_id
JOIN boende ON boende.id = rum.boende_id
WHERE bokning.id = @bokning_id
    AND bokning.kund_id = @kund_id;

-- Ta bort en bokning
DELETE FROM bokning
WHERE id = @bokning_id
    AND kund_id = @kund_id;
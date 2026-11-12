-- Visa aktuella bokningen och rummets kapacitet
SET @bokning_id = 6;

SELECT bokning.id AS bokningsnummer,
       boende.namn AS boende,
       rum.rumsnummer,
       bokning.antal_personer,
       rum.max_antal_personer
FROM bokning
JOIN rum ON rum.id = bokning.rum_id
JOIN boende ON boende.id = rum.boende_id
WHERE bokning.id = @bokning_id;

-- Uppdateras, men bara om det nya antalet ryms
SET @nytt_antal = 4;
UPDATE bokning
JOIN rum ON rum.id = bokning.rum_id
SET bokning.antal_personer = @nytt_antal
WHERE bokning.id = @bokning_id
    AND rum.max_antal_personer >= @nytt_antal;
SET @kund_id = 6,
    @rum_id = 11,
    @in_check = '2026-12-10',
    @ut_check = '2026-12-12',
    @antal_pers = 2;

-- 1 Betyder att rummet finns, sällskapet får plats och är ledigt. 0 Betyder att något av kraven inte uppfylls
SELECT COUNT(*) AS kan_bokas
FROM rum
WHERE rum.id = @rum_id
    AND rum.max_antal_personer >= @antal_pers
    AND NOT EXISTS(
        SELECT 1 -- Här
        FROM bokning
        WHERE bokning.rum_id = rum.id
            AND bokning.incheckningsdatum < @ut_check
            AND bokning.utcheckningsdatum > @in_check
);

-- Spara bokningen och hämta bokningsnummret
INSERT INTO bokning (kund_id, rum_id, incheckningsdatum, utcheckningsdatum, antal_personer)
VALUES (@kund_id, @rum_id, @in_check, @ut_check, @antal_pers);
SET @bokning_id = LAST_INSERT_ID();

-- Bekräftelse med bokningsnummer och totalpris
SELECT bokning.id AS bokningsnummer,
       boende.namn AS boende,
       rum.rumsnummer,
       bokning.incheckningsdatum,
       bokning.utcheckningsdatum,
       bokning.antal_personer,
       DATEDIFF(bokning.utcheckningsdatum, bokning.incheckningsdatum) AS antal_natter,
       DATEDIFF(bokning.utcheckningsdatum, bokning.incheckningsdatum) * rum.pris_per_natt AS totalpris
FROM bokning
JOIN rum ON rum.id = bokning.rum_id
JOIN boende ON boende.id = rum.boende_id
WHERE bokning.id = @bokning_id;
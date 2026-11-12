SET @kund_id = 6;

-- VIsa bokningen med bokningsnummer, boende, ort, rumsnummer
-- In och ut checkning, antal personer, antal nätter och totalpris
SELECT bokning.id AS bokningsnummer,
       boende.namn AS boende,
       boende.ort AS ort,
       rum.rumsnummer,
       bokning.incheckningsdatum,
       bokning.utcheckningsdatum,
       bokning.antal_personer,
       DATEDIFF(bokning.utcheckningsdatum, bokning.incheckningsdatum) AS antal_natter,
       DATEDIFF(bokning.utcheckningsdatum, bokning.incheckningsdatum) * rum.pris_per_natt AS totalpris
FROM bokning
JOIN rum ON rum.id = bokning.rum_id
JOIN boende ON boende.id = rum.boende_id
WHERE bokning.kund_id = @kund_id
ORDER BY bokning.incheckningsdatum;
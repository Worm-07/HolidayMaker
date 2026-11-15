SELECT COUNT(*) AS antal_kunder
FROM kund;

-- Visar en lista på befintliga kunder
SELECT id, namn, email, telefonnummer
FROM kund
ORDER BY namn;

-- Registrera en ny kund
SET @namn = 'Emil Chau',
    @email = 'emil.chau@exempel.com',
    @telefon = '070-200 36 12';
INSERT INTO kund ( namn, email, telefonnummer)
VALUES (@namn, @email, @telefon);

-- Sök efter en kund
SET @sok = '';
SELECT id, namn, email, telefonnummer
FROM kund
WHERE namn LIKE CONCAT('%', @sok, '%')
OR email LIKE CONCAT ('%', @sok, '%')
ORDER BY namn;

-- Välj kund via id
SET @kund_id = 6;
SELECT id,namn,email, telefonnummer
FROM kund
WHERE id = @kund_id;
-- Test data
-- 0 måste inte ha pool, 1 måste ha pool
-- NUll, inget filter
SET @ort = 'Trelleborg',
    @in_check = '2026-12-10',
    @ut_check = '2026-12-12',
    @antal_pers = 2,
    @kraver_pool = 0,
    @kraver_restaurang = 0,
    @max_strand = NULL,
    @max_centrum = NULL;

-- Kollar så att det är på rätt ort och håller sällskapet. Och dom övriga kraven
SELECT boende.namn, boende.ort, rum.id AS rum_id, rum.rumsnummer,
       rum.max_antal_personer, rum.pris_per_natt, boende.omdome
FROM rum
JOIN boende ON boende.id = rum.boende_id
WHERE boende.ort = @ort
    AND rum.max_antal_personer >= @antal_pers
    AND (@kraver_pool = 0 OR boende.pool = 1)
    AND (@kraver_restaurang = 0 OR boende.restaurang = 1)
    AND (@max_strand IS NULL OR boende.avstand_till_strand_m <= @max_strand)
    AND (@max_centrum IS NULL OR boende.avstand_till_centrum_m <= @max_centrum)
    AND NOT EXISTS(
        SELECT 1
        FROM bokning
        WHERE bokning.rum_id = rum.id
            AND bokning.incheckningsdatum < @ut_check
            AND bokning.utcheckningsdatum > @in_check
)
-- Sorterar dom
ORDER BY boende.omdome DESC ,rum.pris_per_natt ASC;
DELIMITER //
CREATE OR REPLACE PROCEDURE `Personal`.`usp_GetMetalMissing`(
	IN startDate varchar(10),
	IN endDate varchar(10),	
    IN metalName varchar(15)
)
BEGIN
WITH Calendar AS (
    SELECT
        CURDATE() - INTERVAL (a.a + (10 * b.a) + (100 * c.a)) DAY AS RateDate
    FROM
        (SELECT 0 AS a UNION ALL SELECT 1 UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4 
         UNION ALL SELECT 5 UNION ALL SELECT 6 UNION ALL SELECT 7 UNION ALL SELECT 8 UNION ALL SELECT 9) a
    CROSS JOIN
        (SELECT 0 AS a UNION ALL SELECT 1 UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4 
         UNION ALL SELECT 5 UNION ALL SELECT 6 UNION ALL SELECT 7 UNION ALL SELECT 8 UNION ALL SELECT 9) b
    CROSS JOIN
        (SELECT 0 AS a UNION ALL SELECT 1 UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4 
         UNION ALL SELECT 5 UNION ALL SELECT 6 UNION ALL SELECT 7 UNION ALL SELECT 8 UNION ALL SELECT 9) c
    WHERE
        CURDATE() - INTERVAL (a.a + (10 * b.a) + (100 * c.a)) DAY >= startDate
    AND
        CURDATE() - INTERVAL (a.a + (10 * b.a) + (100 * c.a)) DAY <= endDate
)
SELECT
    c.RateDate
FROM
    Calendar c
LEFT JOIN
    MetalPrices er ON c.RateDate = er.RateDate AND er.Metal = metalName
WHERE
    WEEKDAY(c.RateDate) < 5             	-- Only weekdays
    AND fn_IsHoliday(c.RateDate) = false	-- Not a U.S. holiday
    AND er.RateDate IS NULL;              	-- No data in MetalPrices
END;
//

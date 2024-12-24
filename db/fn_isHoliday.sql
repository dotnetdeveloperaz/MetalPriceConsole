DELIMITER //
CREATE OR REPLACE FUNCTION `personal`.`fn_IsHoliday`(date DATE) RETURNS tinyint(1)
    DETERMINISTIC
BEGIN
    DECLARE year INT DEFAULT YEAR(date);
    DECLARE month INT DEFAULT MONTH(date);
    DECLARE day INT DEFAULT DAY(date);
    DECLARE dayName VARCHAR(12) DEFAULT DAYNAME(date);
    DECLARE nthWeekDay INT DEFAULT CEIL(DAY(date) / 7);
    DECLARE isThursday BOOLEAN DEFAULT (dayName = 'Thursday');
    DECLARE isFriday BOOLEAN DEFAULT (dayName = 'Friday');
    DECLARE isSaturday BOOLEAN DEFAULT (dayName = 'Saturday');
    DECLARE isSunday BOOLEAN DEFAULT (dayName = 'Sunday');
    DECLARE isMonday BOOLEAN DEFAULT (dayName = 'Monday');
    DECLARE isWeekend BOOLEAN DEFAULT (isSaturday OR isSunday);
    
    -- New Year's Day
    IF (month = 12 AND day = 31 AND isFriday) THEN RETURN TRUE; END IF;
    IF (month = 1 AND day = 1 AND NOT isWeekend) THEN RETURN TRUE; END IF;
    IF (month = 1 AND day = 2 AND isMonday) THEN RETURN TRUE; END IF;
    
    -- Martin Luther King Jr. Day (3rd Monday in January)
    IF (month = 1 AND isMonday AND nthWeekDay = 3) THEN RETURN TRUE; END IF;
    
    -- Presidents' Day (3rd Monday in February)
    IF (month = 2 AND isMonday AND nthWeekDay = 3) THEN RETURN TRUE; END IF;
    
    -- Memorial Day (last Monday in May)
    IF (month = 5 AND isMonday AND MONTH(DATE_ADD(date, INTERVAL 7 DAY)) = 6) THEN RETURN TRUE; END IF;
   
    -- Juneteenth (June 19th)
    IF (month = 6 AND day = 19) THEN RETURN TRUE; END IF;
    
    -- Independence Day (July 4)
    IF (month = 7 AND day = 3 AND isFriday) THEN RETURN TRUE; END IF;
    IF (month = 7 AND day = 4 AND NOT isWeekend) THEN RETURN TRUE; END IF;
    IF (month = 7 AND day = 5 AND isMonday) THEN RETURN TRUE; END IF;
    
    -- Labor Day (1st Monday in September)
    IF (month = 9 AND isMonday AND nthWeekDay = 1) THEN RETURN TRUE; END IF;
    
    -- Columbus Day (2nd Monday in October)
    IF (month = 10 AND isMonday AND nthWeekDay = 2) THEN RETURN TRUE; END IF;
    
    -- Veterans Day (November 11)
    IF (month = 11 AND day = 10 AND isFriday) THEN RETURN TRUE; END IF;
    IF (month = 11 AND day = 11 AND NOT isWeekend) THEN RETURN TRUE; END IF;
    IF (month = 11 AND day = 12 AND isMonday) THEN RETURN TRUE; END IF;
    
    -- Thanksgiving Day (4th Thursday in November)
    IF (month = 11 AND isThursday AND nthWeekDay = 4) THEN RETURN TRUE; END IF;
    
    -- Christmas Day (December 25)
    IF (month = 12 AND day = 24 AND isFriday) THEN RETURN TRUE; END IF;
    IF (month = 12 AND day = 25 AND NOT isWeekend) THEN RETURN TRUE; END IF;
    IF (month = 12 AND day = 26 AND isMonday) THEN RETURN TRUE; END IF;

    RETURN FALSE;
END;
//
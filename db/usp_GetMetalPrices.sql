CREATE DEFINER=`root`@`%` PROCEDURE `Personal`.`usp_GetMetalPrices`(
	IN startDate varchar(10),
	IN endDate varchar(10),
	IN metalName varchar(15),
	IN baseCurrency varchar(3)
)
BEGIN
	SELECT 
		Metal
		, Currency 
		, Price 
		, PrevPriceClose 
		, RateDate 
		, `Timestamp`
		, Chg
		, ChgPct 
		, Price_Gram_24k 
		, Price_Gram_22k 
		, Price_Gram_21k 
		, Price_Gram_20k 
		, Price_Gram_18k 
	FROM MetalPrices
	WHERE RateDate >= startDate
	AND RateDate <= endDate
	AND FIND_IN_SET(Metal, metalName) > 0
	AND Currency = baseCurrency;
END;
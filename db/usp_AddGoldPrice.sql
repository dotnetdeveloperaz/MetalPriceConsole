CREATE DEFINER=`root`@`%` PROCEDURE `Personal`.`usp_AddGoldPrice`(
	IN price double, IN prev_price double,
	IN ratedate date, chg double, chg_pct double)
BEGIN
	INSERT INTO GoldRates 
		(Price, PrevPriceClose, RateDate, Chg, ChgPct)
		VALUES (price, prev_price, ratedate, chg, chg_pct)
	ON DUPLICATE KEY UPDATE 
		Price = price
		, PrevPriceClose = Prev_Price 
		, Chg = chg
		, ChgPct = chg_pct;
END;

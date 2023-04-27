CREATE DEFINER=`root`@`%` PROCEDURE `personal`.`usp_AddGoldPrice`(
	IN price double, IN prev_price double,
	IN ratedate date, 
	IN chg double, 
	IN chg_pct double,
	IN price_gram_24k double,
	IN price_gram_22k double,
	IN price_gram_21k double,
    IN price_gram_20k double,
    IN price_gram_18k double
)
BEGIN
	INSERT INTO GoldPrices 
		(Price, PrevPriceClose, RateDate, Chg, ChgPct, price_gram_24k, price_gram_22k, price_gram_21k, price_gram_20k, price_gram_18k)
		VALUES (price, prev_price, ratedate, chg, chg_pct, price_gram_24k, price_gram_22k, price_gram_21k, price_gram_20k, price_gram_18k
)
	ON DUPLICATE KEY UPDATE 
		Price = price
		, PrevPriceClose = Prev_Price 
		, Chg = chg
		, ChgPct = chg_pct
		, Price_Gram_24k = price_gram_24k
		, Price_Gram_22k = price_gram_22k
		, Price_Gram_21k = price_gram_21k
		, Price_Gram_20k = price_gram_20k
		, Price_Gram_18k = price_gram_18k;
END;

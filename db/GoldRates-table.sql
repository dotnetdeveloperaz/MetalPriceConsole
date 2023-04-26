-- personal.goldprices definition

CREATE TABLE `goldprices` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `Price` double NOT NULL,
  `PrevPriceClose` double NOT NULL,
  `RateDate` date NOT NULL,
  `Chg` double NOT NULL,
  `ChgPct` double NOT NULL,
  `AddDate` timestamp NOT NULL DEFAULT current_timestamp(),
  `Price_Gram_24k` double DEFAULT NULL,
  `Price_Gram_22k` double DEFAULT NULL,
  `Price_Gram_21k` double DEFAULT NULL,
  `Price_Gram_20k` double DEFAULT NULL,
  `Price_Gram_18k` double DEFAULT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `GoldPrices_UN` (`RateDate`)
) ENGINE=InnoDB AUTO_INCREMENT=1292 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
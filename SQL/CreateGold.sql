-- personal.metalPrices definition

CREATE TABLE `metalPrices` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `Metal` varchar(20) NOT NULL,
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
  UNIQUE KEY `metalPrices_UN` (`RateDate`,`Metal`)
) ENGINE=InnoDB AUTO_INCREMENT=1375 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
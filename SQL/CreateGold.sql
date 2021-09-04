-- Personal.GoldRates definition
CREATE TABLE `GoldPrices` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `Price` double NOT NULL,
  `PrevPriceClose` double NOT NULL,
  `RateDate` date NOT NULL,
  `Chg` double NOT NULL,
  `ChgPct` double NOT NULL,
  `AddDate` timestamp NOT NULL DEFAULT current_timestamp(),
  PRIMARY KEY (`Id`),
  UNIQUE KEY `GoldPrices_UN` (`RateDate`)
) ENGINE=InnoDB AUTO_INCREMENT=590 DEFAULT CHARSET=utf8mb4;
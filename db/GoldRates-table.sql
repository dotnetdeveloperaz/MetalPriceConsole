-- Personal.GoldRates definition

CREATE TABLE `GoldRates` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `Price` double NOT NULL,
  `PrevPriceClose` double NOT NULL,
  `RateDate` date NOT NULL,
  `Chg` double NOT NULL,
  `ChgPct` double NOT NULL,
  `AddDate` timestamp NOT NULL DEFAULT current_timestamp(),
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB AUTO_INCREMENT=572 DEFAULT CHARSET=utf8mb4;

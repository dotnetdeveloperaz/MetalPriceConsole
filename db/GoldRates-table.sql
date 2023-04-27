<<<<<<< HEAD
-- personal.metalprices definition

CREATE TABLE `metalprices` (
=======
-- personal.goldprices definition

CREATE TABLE `goldprices` (
>>>>>>> main
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `Metal` varchar(20) NOT NULL,
  `Currency` char(3) NOT NULL,
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
<<<<<<< HEAD
  UNIQUE KEY `GoldPrices_UN` (`RateDate`,`Metal`,`Currency`)
) ENGINE=InnoDB AUTO_INCREMENT=1375 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
=======
  UNIQUE KEY `GoldPrices_UN` (`RateDate`)
) ENGINE=InnoDB AUTO_INCREMENT=1292 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
>>>>>>> main

# ************************************************************
# Sequel Pro SQL dump
# Version 3348
#
# http://www.sequelpro.com/
# http://code.google.com/p/sequel-pro/
#
# Host: 127.0.0.1 (MySQL 5.1.53)
# Database: QuadMap
# Generation Time: 2011-07-13 20:55:01 +0100
# ************************************************************


/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;


# Dump of table FaceTypes
# ------------------------------------------------------------

DROP TABLE IF EXISTS `FaceTypes`;

CREATE TABLE `FaceTypes` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `Name` char(100) DEFAULT NULL,
  `NameShort` char(6) DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=MyISAM AUTO_INCREMENT=10 DEFAULT CHARSET=latin1;

LOCK TABLES `FaceTypes` WRITE;
/*!40000 ALTER TABLE `FaceTypes` DISABLE KEYS */;

INSERT INTO `FaceTypes` (`id`, `Name`, `NameShort`)
VALUES
	(1,'Light animations','A,B'),
	(2,'People\'s faces','C,D'),
	(3,'People\'s faces','E'),
	(4,'People\'s faces','F'),
	(5,'Big graphics','G land'),
	(6,'Big graphics','G port'),
	(7,'Big graphics','H land'),
	(8,'Big graphics','I port'),
	(9,'Mask','Mask');

/*!40000 ALTER TABLE `FaceTypes` ENABLE KEYS */;
UNLOCK TABLES;


# Dump of table Quads
# ------------------------------------------------------------

DROP TABLE IF EXISTS `Quads`;

CREATE TABLE `Quads` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `iProjector` int(11) NOT NULL DEFAULT '0',
  `iType` int(11) NOT NULL DEFAULT '0',
  `corner0x` float NOT NULL DEFAULT '0',
  `corner0y` float NOT NULL DEFAULT '0',
  `corner1x` float NOT NULL DEFAULT '0',
  `corner1y` float NOT NULL DEFAULT '0',
  `corner2x` float NOT NULL DEFAULT '0',
  `corner2y` float NOT NULL DEFAULT '0',
  `corner3x` float NOT NULL DEFAULT '0',
  `corner3y` float NOT NULL DEFAULT '0',
  `zOrder` int(11) NOT NULL DEFAULT '0',
  `Created` timestamp NOT NULL DEFAULT '0000-00-00 00:00:00',
  `Updated` timestamp NOT NULL DEFAULT '0000-00-00 00:00:00' ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`)
) ENGINE=MyISAM AUTO_INCREMENT=32 DEFAULT CHARSET=latin1;

LOCK TABLES `Quads` WRITE;
/*!40000 ALTER TABLE `Quads` DISABLE KEYS */;

INSERT INTO `Quads` (`id`, `iProjector`, `iType`, `corner0x`, `corner0y`, `corner1x`, `corner1y`, `corner2x`, `corner2y`, `corner3x`, `corner3y`, `zOrder`, `Created`, `Updated`)
VALUES
	(19,0,0,-0.597846,0.545866,-0.499331,0.578125,-0.586214,0.305929,-0.491322,0.345284,0,'0000-00-00 00:00:00','2011-01-18 13:16:31'),
	(20,0,0,-0.691181,0.587633,-0.59685,0.545848,-0.67224,0.342619,-0.586343,0.307217,0,'0000-00-00 00:00:00','2011-01-18 13:16:34'),
	(12,3,0,-0.683297,0.891932,-0.156449,0.882064,-0.791756,0.486565,-0.479936,0.477753,0,'0000-00-00 00:00:00','2011-01-14 18:11:07'),
	(14,3,0,-0.778557,1,0.621446,0.950817,-0.628557,-0.299186,0.621446,-0.299186,0,'0000-00-00 00:00:00','2011-07-12 15:47:38'),
	(16,3,0,-0.546954,-0.423156,-0.037458,-0.383361,-0.62821,-0.820535,0.163916,-0.886,0,'0000-00-00 00:00:00','2011-07-12 15:48:01'),
	(21,0,0,-0.585666,0.61009,-0.499119,0.578595,-0.691859,0.586646,-0.596699,0.546437,0,'0000-00-00 00:00:00','2011-01-18 13:19:45'),
	(22,0,1,-0.869594,0.602181,-0.75949,0.622966,-0.866224,0.379727,-0.739268,0.337596,0,'0000-00-00 00:00:00','2011-01-18 13:16:48'),
	(24,0,1,-0.315548,0.808217,0.356898,0.74362,-0.540768,0.416778,-0.0493997,0.108278,0,'0000-00-00 00:00:00','2011-07-12 15:52:22'),
	(25,0,1,-0.0531918,-0.295393,0.427148,-0.153504,-0.386061,-0.759214,0.541,-0.801628,0,'0000-00-00 00:00:00','2011-07-12 15:36:45'),
	(27,0,0,-0.686,0.198,-0.376,0.12,-0.78,-0.458,-0.328,-0.454,0,'0000-00-00 00:00:00','2011-07-12 15:46:08');

/*!40000 ALTER TABLE `Quads` ENABLE KEYS */;
UNLOCK TABLES;



/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;
/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;

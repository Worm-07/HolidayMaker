-- Skapar databasen "public" med tabeller och testdata.
CREATE DATABASE IF NOT EXISTS public CHARACTER SET utf8mb4;
USE public;

/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;
DROP TABLE IF EXISTS `boende`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `boende` (
  `id` bigint NOT NULL AUTO_INCREMENT,
  `namn` varchar(100) NOT NULL,
  `ort` varchar(100) NOT NULL,
  `pool` tinyint(1) NOT NULL DEFAULT '0',
  `restaurang` tinyint(1) NOT NULL DEFAULT '0',
  `avstand_till_strand_m` int NOT NULL,
  `avstand_till_centrum_m` int NOT NULL,
  `omdome` tinyint NOT NULL,
  PRIMARY KEY (`id`),
  CONSTRAINT `chk_boende_centrum` CHECK ((`avstand_till_centrum_m` >= 0)),
  CONSTRAINT `chk_boende_omdome` CHECK ((`omdome` between 1 and 5)),
  CONSTRAINT `chk_boende_strand` CHECK ((`avstand_till_strand_m` >= 0))
) ENGINE=InnoDB AUTO_INCREMENT=6 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

INSERT INTO `boende` VALUES (1,'Willow','Trelleborg',1,1,500,100,4),(2,'Scandic Triangeln','Malmö',0,1,2000,50,4),(3,'Hotell Stavstensgården','Trelleborg',0,1,210,5500,5),(4,'Atlantis-The_Palm','Dubai',1,1,200,26100,5),(5,'Hotel_N_Hostel','Malmö',0,0,2900,400,3);
DROP TABLE IF EXISTS `bokning`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `bokning` (
  `id` bigint NOT NULL AUTO_INCREMENT,
  `kund_id` bigint NOT NULL,
  `rum_id` bigint NOT NULL,
  `incheckningsdatum` date NOT NULL,
  `utcheckningsdatum` date NOT NULL,
  `antal_personer` tinyint NOT NULL,
  PRIMARY KEY (`id`),
  KEY `fk_bokning_kund` (`kund_id`),
  KEY `fk_bokning_rum` (`rum_id`),
  CONSTRAINT `fk_bokning_kund` FOREIGN KEY (`kund_id`) REFERENCES `kund` (`id`),
  CONSTRAINT `fk_bokning_rum` FOREIGN KEY (`rum_id`) REFERENCES `rum` (`id`),
  CONSTRAINT `chk_bokning_datum` CHECK ((`utcheckningsdatum` > `incheckningsdatum`)),
  CONSTRAINT `chk_bokning_personer` CHECK ((`antal_personer` > 0))
) ENGINE=InnoDB AUTO_INCREMENT=29 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

INSERT INTO `bokning` VALUES (1,1,10,'2026-12-10','2026-12-13',2),(2,2,10,'2026-12-13','2026-12-15',2),(3,3,1,'2026-12-05','2026-12-20',2),(4,4,3,'2026-12-21','2026-12-27',4),(5,5,4,'2026-12-22','2026-12-28',4),(6,1,8,'2026-12-03','2026-12-08',3),(7,2,2,'2026-12-01','2026-12-04',1),(8,3,5,'2026-12-06','2026-12-07',1),(9,4,6,'2026-12-11','2026-12-14',1),(10,5,7,'2026-12-15','2026-12-22',2),(11,1,9,'2026-12-02','2026-12-06',1),(12,2,11,'2026-12-18','2026-12-24',2),(13,3,12,'2026-12-01','2026-12-08',2),(14,4,13,'2026-12-09','2026-12-12',2),(15,5,14,'2026-12-20','2026-12-27',4),(16,1,15,'2026-12-24','2026-12-31',4),(17,2,16,'2026-12-04','2026-12-10',4),(18,3,17,'2026-12-27','2026-12-30',3),(19,4,18,'2026-12-08','2026-12-09',1),(20,5,19,'2026-12-12','2026-12-19',2),(21,1,20,'2026-12-01','2026-12-03',2),(22,2,8,'2026-12-12','2026-12-19',4),(23,3,2,'2026-12-17','2026-12-24',1),(24,4,9,'2026-12-26','2026-12-31',1),(25,5,13,'2026-12-20','2026-12-23',1),(27,6,11,'2026-12-10','2026-12-12',2);
DROP TABLE IF EXISTS `kund`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `kund` (
  `id` bigint NOT NULL AUTO_INCREMENT,
  `namn` varchar(100) NOT NULL,
  `email` varchar(255) NOT NULL,
  `telefonnummer` varchar(20) NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `uq_kund_email` (`email`)
) ENGINE=InnoDB AUTO_INCREMENT=11 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

INSERT INTO `kund` VALUES (1,'Anna Lindqvist','anna.lindqvist@exempel.com','070-123 45 67'),(2,'Johan Berg','johan.berg@exempel.com','073-234 56 78'),(3,'Sten Karlsson','sten.karlsson@exempel.com','076-345 67 89'),(4,'Erik Nilsson','erik.nilsson@exempel.com','072-456 78 90'),(5,'Maria Andersson','maria.andersson@exempel.com','079-567 89 01'),(6,'Emil Chau','emil.chau@exempel.com','070-200 36 12'),(7,'Lisa Kopia','lisa.holm@exempel.com','070-000 00 00'),(9,'Emilia Chau','emil2.chau@exempel.com','070-200 36 13'),(10,'Ebba','ebba.andersson@exempel.com','070-396 73 59');
DROP TABLE IF EXISTS `rum`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `rum` (
  `id` bigint NOT NULL AUTO_INCREMENT,
  `boende_id` bigint NOT NULL,
  `rumsnummer` int NOT NULL,
  `max_antal_personer` int NOT NULL,
  `pris_per_natt` int NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `uq_rum_boende_rumsnummer` (`boende_id`,`rumsnummer`),
  CONSTRAINT `fk_rum_boende` FOREIGN KEY (`boende_id`) REFERENCES `boende` (`id`),
  CONSTRAINT `chk_rum_kapacitet` CHECK ((`max_antal_personer` > 0)),
  CONSTRAINT `chk_rum_pris` CHECK ((`pris_per_natt` > 0))
) ENGINE=InnoDB AUTO_INCREMENT=21 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

INSERT INTO `rum` VALUES (1,1,701,2,1450),(2,1,804,1,1200),(3,1,810,4,1900),(4,1,1101,6,7800),(5,2,801,1,1300),(6,2,802,1,1300),(7,2,902,2,1600),(8,2,1001,4,2200),(9,3,1,1,1100),(10,3,2,2,1700),(11,3,3,2,1700),(12,4,801,2,9300),(13,4,802,2,9300),(14,4,901,4,12400),(15,4,902,4,12400),(16,4,903,4,12400),(17,4,1101,3,58960),(18,5,5,1,800),(19,5,6,2,1100),(20,5,7,2,1100);
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;


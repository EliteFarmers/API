START TRANSACTION;
ALTER TABLE "GuideVersions" ADD "ConcurrencyVersion" integer NOT NULL DEFAULT 0;

ALTER TABLE "BazaarProductSummaries" ADD "AvgTopBuyOrderPrice" double precision NOT NULL DEFAULT 0.0;

ALTER TABLE "BazaarProductSummaries" ADD "AvgTopSellOrderPrice" double precision NOT NULL DEFAULT 0.0;

ALTER TABLE "BazaarProductSummaries" ADD "TopBuyOrderPrice" double precision NOT NULL DEFAULT 0.0;

ALTER TABLE "BazaarProductSummaries" ADD "TopSellOrderPrice" double precision NOT NULL DEFAULT 0.0;

ALTER TABLE "BazaarProductSnapshots" ADD "TopBuyOrderPrice" double precision NOT NULL DEFAULT 0.0;

ALTER TABLE "BazaarProductSnapshots" ADD "TopSellOrderPrice" double precision NOT NULL DEFAULT 0.0;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260306031705_AddGuideConcurrency', '10.0.2');

COMMIT;


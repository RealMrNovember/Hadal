-- Reverses the kingdom migration (docs/39 §3.1). Only valid while every world row still belongs to Kingdom #1
-- ('w1'); it stops otherwise. Take a backup first. Run with: psql -v ON_ERROR_STOP=1 -f kingdoms_down.sql
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM hadal.world_bases WHERE world_id <> 'w1')
       OR EXISTS (SELECT 1 FROM hadal.world_nodes WHERE world_id <> 'w1')
       OR EXISTS (SELECT 1 FROM hadal.world_regions WHERE world_id <> 'w1') THEN
        RAISE EXCEPTION 'Rows outside Kingdom #1 exist: move or delete them before reversing.';
    END IF;

    IF EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'world_bases_world_x_y_key') THEN
        ALTER TABLE hadal.world_bases DROP CONSTRAINT world_bases_world_x_y_key;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'world_bases_x_y_key') THEN
        ALTER TABLE hadal.world_bases ADD CONSTRAINT world_bases_x_y_key UNIQUE (x, y);
    END IF;
    IF (SELECT array_length(conkey, 1) FROM pg_constraint WHERE conname = 'world_nodes_pkey') = 2 THEN
        ALTER TABLE hadal.world_nodes DROP CONSTRAINT world_nodes_pkey;
        ALTER TABLE hadal.world_nodes ADD CONSTRAINT world_nodes_pkey PRIMARY KEY (id);
    END IF;
    IF (SELECT array_length(conkey, 1) FROM pg_constraint WHERE conname = 'world_regions_pkey') = 2 THEN
        ALTER TABLE hadal.world_regions DROP CONSTRAINT world_regions_pkey;
        ALTER TABLE hadal.world_regions ADD CONSTRAINT world_regions_pkey PRIMARY KEY (region);
    END IF;

    DROP TABLE IF EXISTS hadal.kingdom_members;
    DROP TABLE IF EXISTS hadal.kingdoms;
END $$;

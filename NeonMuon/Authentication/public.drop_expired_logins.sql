-- PROCEDURE: public.drop_expired_logins()

-- DROP PROCEDURE IF EXISTS public.drop_expired_logins();

CREATE OR REPLACE PROCEDURE public.drop_expired_logins() 
LANGUAGE 'plpgsql'
AS $BODY$
DECLARE
	rol record;
BEGIN
	FOR rol IN
		SELECT format('drop role %I', rolname) stmt,
			rolname,
			rolvaliduntil
		FROM pg_catalog.pg_roles
		WHERE rolname LIKE '%:%' AND rolvaliduntil < CURRENT_TIMESTAMP 
	LOOP
		RAISE NOTICE 'dropping expired role	%	%', rol.rolname, rol.rolvaliduntil;
		EXECUTE rol.stmt;
	END LOOP;
END;
$BODY$;

ALTER PROCEDURE public.drop_expired_logins() OWNER TO pg_database_owner;
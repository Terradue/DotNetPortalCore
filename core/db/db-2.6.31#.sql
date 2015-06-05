USE $MAIN$;

/*****************************************************************************/

INSERT INTO config (id_section, pos, name, type, source, caption, hint, value, optional) VALUES
    (1, 0, 'ForceReload', 'bool', NULL, 'Force Configuration Reload by Agent', 'If checked, the configuration is reloaded by the agent on its next run', 'true', true)
;

/*****************************************************************************/

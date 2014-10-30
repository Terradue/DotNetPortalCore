/*****************************************************************************/

-- Changing configuration for log conservation ... \
UPDATE config SET name='ChangeLogStart', caption='Change Log Start Time', hint='Enter the start date/time of the period for which the change log must be conserved, relative to the current date(e.g. -3M, -2M D=1)', value='-3M' WHERE name='ChangeLogValidity';
-- RESULT

/*****************************************************************************/

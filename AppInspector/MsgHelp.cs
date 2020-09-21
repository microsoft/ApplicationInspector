// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using Microsoft.ApplicationInspector.Commands.Properties;
using System;

namespace Microsoft.ApplicationInspector.Commands
{
    public static class MsgHelp
    {
        /// <summary>
        /// Maps enum values to resource strings for ensuring values exists at compile time
        /// </summary>
        public enum ID
        {
            ANALYZE_COMPRESSED_FILETYPE,
            ANALYZE_FILES_PROCESSED_PCNT,
            ANALYZE_NOPATTERNS,
            ANALYZE_NOSUPPORTED_FILETYPES,
            ANALYZE_UNCOMPRESSED_FILETYPE,
            ANALYZE_UNSUPPORTED_COMPR_TYPE,
            ANALYZE_FILESIZE_SKIPPED,
            ANALYZE_EXCLUDED_TYPE_SKIPPED,
            ANALYZE_LANGUAGE_NOTFOUND,
            ANALYZE_COMPRESSED_FILESIZE_WARN,
            ANALYZE_COMPRESSED_PROCESSING,
            ANALYZE_COMPRESSED_ERROR,
            ANALYZE_FILE_TYPE_OPEN,
            ANALYZE_REPORTSIZE_WARN,
            ANALYZE_NODUPLICATES_HTML_FORMAT,
            ANALYZE_SIMPLETAGS_HTML_FORMAT,
            ANALYZE_HTML_EXTENSION,
            CMD_NO_OUTPUT,
            CMD_PREPARING_REPORT,
            CMD_COMPLETED,
            CMD_CRITICAL_FILE_ERR,
            CMD_INVALID_ARG_VALUE,
            CMD_INVALID_FILE_OR_DIR,
            CMD_REPORT_DONE,
            CMD_REQUIRED_ARG_MISSING,
            CMD_RUNNING,
            CMD_INVALID_RULE_PATH,
            CMD_NORULES_SPECIFIED,
            CMD_INVALID_LOG_PATH,
            CMD_VIEW_OUTPUT_FILE,
            CMD_REMINDER_CHECK_LOG,
            TAGDIFF_NO_TAGS_FOUND,
            TAGDIFF_RESULTS_DIFFER,
            TAGDIFF_RESULTS_GAP,
            TAGDIFF_RESULTS_TEST_TYPE,
            TAGDIFF_SAME_FILE_ARG,
            TAGDIFF_RESULTS_SUCCESSS,
            TAGDIFF_RESULTS_FAIL,
            TAGTEST_RESULTS_NONE,
            TAGTEST_RESULTS_TAGS_FOUND,
            TAGTEST_RESULTS_TAGS_MISSING,
            TAGTEST_RESULTS_TEST_TYPE,
            TAGTEST_RESULTS_SUCCESS,
            TAGTEST_RESULTS_FAIL,
            VERIFY_RULE_LOADFILE_FAILED,
            VERIFY_RULES_RESULTS_FAIL,
            VERIFY_RULES_NULLID_FAIL,
            VERIFY_RULES_REGEX_FAIL,
            VERIFY_RULES_LANGUAGE_FAIL,
            VERIFY_RULES_CONDITION_FAIL,
            VERIFY_RULES_DUPLICATEID_FAIL,
            VERIFY_RULES_RESULTS_SUCCESS,
            VERIFY_RULES_NO_CLI_DEFAULT,
            RUNTIME_ERROR_NAMED,
            RUNTIME_ERROR_UNNAMED,
            RUNTIME_ERROR_PRELOG,
            BROWSER_ENVIRONMENT_VAR,
            BROWSER_START_FAIL,
            BROWSER_START_SUCCESS,
            PACK_MISSING_OUTPUT_ARG,
            PACK_RULES_NO_CLI_DEFAULT,
            PACK_RULES_NO_DEFAULT,
        };

        public static string GetString(MsgHelp.ID id)
        {
            string result;
            try
            {
                result = Resources.ResourceManager.GetString(id.ToString())??"";
            }
            catch (Exception e)
            {
                string error = string.Format("Unable to locate requested string resource {0}", id);
                error += e.Message + "\n" + e.StackTrace;
                throw new Exception(error);
            }

            return result ?? "";
        }

        public static string FormatString(MsgHelp.ID id, params object[]? parameters)
        {
            return string.Format(GetString(id), parameters ?? new object[] { });
        }

        public static string FormatString(MsgHelp.ID id, int value)
        {
            return string.Format(GetString(id), value);
        }
    }
}
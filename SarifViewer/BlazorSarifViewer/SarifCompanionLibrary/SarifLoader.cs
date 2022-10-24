using Microsoft.CodeAnalysis.Sarif;
using System;
using System.Linq;

namespace SarifCompanionLibrary
{
    public class SarifLoader
    {
        string _sarifFilePath;
        SarifLog _sarifLog;

        public SarifLoader(string sarifFilePath)
        {
            _sarifFilePath = sarifFilePath;
            _sarifLog = LoadFileDefferedInternal();
        }

        SarifLog LoadFileDefferedInternal()
        {
            var sarifLog = SarifLog.LoadDeferred(_sarifFilePath);
            return sarifLog;
        }


        public string[] GetRuns()
        {
            return _sarifLog.Runs.Select((run) => run.Tool.Driver.Name).ToArray();
        }

        public string[] GetRules(string tool)
        {
            var run = SelectRun(tool);
            if (run != default)
            {
                return run.Tool.Driver.Rules.Select((rule) => rule.Name).ToArray();
            }

            return default;
        }

        public string[] GetResultKinds()
        {
            return Enum.GetNames(typeof(ResultKind));
        }

        public string[] GetFailureLevels()
        {
            return Enum.GetNames(typeof(FailureLevel));
        }

        public int GetResultCount(string tool)
        {
            var run = SelectRun(tool);
            return run.Results.Count;
        }

        public string[] GetTags(string tool)
        {
            var run = SelectRun(tool);
            return run.Tool.Driver.Tags.ToArray();
        }

        Run SelectRun(string tool)
        {
            return _sarifLog.Runs.Where((run) => run.Tool.Driver.Name == tool).FirstOrDefault(); // may fail if both runs by the same tool
        }
    }
}

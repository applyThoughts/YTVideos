using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YTVidoes.Core
{

    public class ServiceResult
    {
        internal const string DefaultSuccessMessage = "Success";
        private readonly List<ValidationResult> _errors = new List<ValidationResult>();
        private readonly List<ValidationResult> _warnings = new List<ValidationResult>();
        private string _successMessage = DefaultSuccessMessage;

        public List<ValidationResult> Errors => _errors;
        public List<ValidationResult> Warnings => _warnings;
        public bool IsValid => !(_errors.Any());

        public string Message
        {
            get => IsValid
                ? _successMessage
                : $"Failed with {_errors.Count} error" + (_errors.Count == 1 ? "" : "s");
            set => _successMessage = value;
        }
        public ServiceResult AddError(string errorMessage, params string[] propertyNames)
        {
            if (errorMessage == null) return this;
            _errors.Add(new ValidationResult(errorMessage, propertyNames));
            return this;
        }
        public ServiceResult AddWarning(string warningMessage, params string[] propertyNames)
        {
            if (warningMessage == null) return this;
            _warnings.Add(new ValidationResult(warningMessage, propertyNames));
            return this;
        }

       
        public void AddValidationResult(ValidationResult validationResult)
        {
            _errors.Add(validationResult);
        }

        public void AddValidationResults(IEnumerable<ValidationResult> validationResults)
        {
            _errors.AddRange(validationResults);
        }
        public ServiceResult CombineStatuses(ServiceResult status)
        {
            if (!status.IsValid)
            {
                _errors.AddRange(status.Errors);
                _warnings.AddRange(status.Warnings);
            }
            if (IsValid && status.Message != DefaultSuccessMessage)
                Message = status.Message;

            return this;
        }

        public string? GetAllErrors(string seperator = "\n")
        {
            return _errors.Any()
                ? string.Join(seperator, Errors)
                : null;
        }
        public string? GetAllWarnings(string seperator = "\n")
        {
            return _warnings.Any()
                ? string.Join(seperator, Warnings)
                : null;
        }
    }

    public class ServiceResult<T> : ServiceResult
    {
        public T Result { get; set; }
    }
}

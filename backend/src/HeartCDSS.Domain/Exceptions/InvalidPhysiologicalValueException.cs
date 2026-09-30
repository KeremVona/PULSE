namespace HeartCDSS.Domain.Exceptions;

/// <summary>
/// Thrown when a physiological vital sign or laboratory metric violates clinical plausibility bounds.
/// </summary>
public class InvalidPhysiologicalValueException : DomainException
{
    public string ParameterName { get; }
    public object AttemptedValue { get; }
    public string ValidRangeDescription { get; }

    public InvalidPhysiologicalValueException(string parameterName, object attemptedValue, string validRangeDescription)
        : base($"Clinical parameter '{parameterName}' value ({attemptedValue}) violates physiological boundaries. Acceptable limits: {validRangeDescription}.")
    {
        ParameterName = parameterName;
        AttemptedValue = attemptedValue;
        ValidRangeDescription = validRangeDescription;
    }
}

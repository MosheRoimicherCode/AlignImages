using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReadMetadata;

// Data structure to hold the job details
public class RotationJob
{
    public string SourcePath { get; set; }
    public string OutputPath { get; set; }
    public int ClockwiseSteps { get; set; } // 0, 1, 2, or 3
}

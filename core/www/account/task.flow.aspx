<%@ Import Namespace="System.IO" %>
<%@ Import Namespace="System.Drawing" %>
<%@ Import Namespace="System.Drawing.Imaging" %>
<%@ Import Namespace="System.Drawing.Drawing2D" %>
<%@ Import Namespace="System.Drawing.Text" %>

<script language="C#" runat="server">
/*
# Module:       sugar/portal-core
# Author:       Terradue Srl
# Last update:  2013-03-19
# File:         /account/task.flow.aspx
# Version:      2.3
# Description:  Draws a graphical flow representation for a task
#
# This document is the property of Terradue and contains information directly
# resulting from knowledge and experience of Terradue.
# Any changes to this code is forbidden without written consent from Terradue Srl
#
# Contact       info@terradue.com
*/

/*****************************************************************************************************************************/

string[] JobStates = {"wait", "prep", "run", "fail", "done"};
int WaitState = 0;
int DoneState = 4;
int[] JobStateStrokeColors = {0, 0, 0, 0, 0};
int[] JobStateStrokeWidths = {0, 0, 0, 0, 0};
int[] JobStateFillColors = {0x4a7bad, 0xcea552, 0xcece52, 0xa53831, 0x63b552};

string JobFontFamily = "Arial"; 
float JobFontSize = 7F; 
int JobFontColor = 0xffffff;
int LabelYOffset = 3;

int VertPadding = 4;                    // Top and bottom padding of resulting image
int HorzPadding = 4;                    // Left and right padding of resulting image
int BoxWidth = 48;                      // Width of job boxes
int BoxHeight = 24;                     // Height of job boxes
int BoxCornerRadius = 4;                // Radius of rounded corners of job boxes (0 = no rounding)
int BoxVertSpacing = 8;                 // Minimum space between two boxes in the same column 
int CurveRadius = 6;                    // Minimum radius of curved path
int HorzShrink = 8;
int BoxToVertDistance = 8;              // Horizontal distance between box right side and vertical path segments
int VertToArrowheadDistance = 8;        // Horizontal distance between vertical path segments and back of arrowhead
float PathStrokeWidth = 2;              // Width of path strokes
int PathColor = 0x808080;               // Color of path strokes and arrowheads
bool DisambiguatePaths = false;         // If true, avoids use of same verticals for paths to make the dependencies clearer
int VertPathsDistance = 4;              // Distance between two vertical sections of paths (applies only if DisambiguatePaths is true)
bool DifferentArrows = true;            // If true, displays arrows as 
        //   * stroked only (if job at cannot yet start because an input job has not yet completed) or 
        //   * filled only (if job at arrowhead is ready to start or has started already) 
        // otherwise displays arrows always as filled only
int FilledArrowheadHalfWidth = 4;       // Half width of filled arrowhead (i.e. distance from path to one of the lateral corners)
int FilledArrowheadLength = 8;          // Length of filled arrowhead (i.e. distance from the back of the arrowhead to the arrow's end point)
int StrokedArrowheadHalfWidth = 3;      // Half width of stroked arrowhead (i.e. distance from path to one of the lateral corners)
int StrokedArrowheadLength = 7;         // Length of stroked arrowhead (i.e. distance from the back of the arrowhead to the arrow's end point)
int StrokedArrowheadStrokeWidth = 2;    // Stroke width of stroked arrowhead

bool ShapeSmoothing = true;             // Switches shape and line antialising on/off for bitmap (PNG) 
bool FontSmoothing = true;              // Switches font antialising on/off for bitmap (PNG) 

int MaxDistributeIterations = 2;
/*****************************************************************************************************************************/

string OutputFormat;
int ImageWidth, ImageHeight, DisplayWidth, DisplayHeight, GridRowHeight, BoxHalfWidth, LabelXOffset, PathYOffset, MaxJobsPerCol, MaxOutJobsPerCol, MinRow, MaxRow;
int StartX, StartY, EndX, EndY, VertSegmentX;
int index, count, next, i, j, k, l;

Job[] jobs = null;
Col[] cols = null;
Path[] paths = null;

/*****************************************************************************************************************************/
void Page_Load(object sender, EventArgs e) {

</script>
<script language="C#" runat="server">

    OutputFormat = GetUrlParameter("format", "", true);
    if (OutputFormat == "") OutputFormat = "svg";

    MaxJobsPerCol = 0;
    MinRow = 0;
    MaxRow = 0;
    
    string[] jobStrings = GetUrlParameter("flow", "", false).Split(';');
    jobs = new Job[jobStrings.Length];
    for (i = 0; i < jobs.Length; i++) {
        jobs[i] = new Job();
        jobs[i].Name = jobStrings[i].Split('|')[0];
        jobs[i].Row = -1;
    }
    
    // Resolve job dependencies
    int[] jobIndexes;
    int pathCount = 0;
    for (i = 0; i < jobs.Length; i++) {
        jobs[i].Col = -1;
        count = 0;
        string[] inputJobs = jobStrings[i].Split('|');
        if (inputJobs.Length < 1) continue;
        jobIndexes = new int[inputJobs.Length - 1];
        for (j = 0; j < jobIndexes.Length; j++) {
            index = -1;
            for (k = 0; k < jobs.Length; k++) {
                if (jobs[k].Name == inputJobs[j + 1]) {
                    index = k;
                    break;
                }
            }
            if (index != -1) count++;
            jobIndexes[j] = index;
        }
        jobs[i].InJobs = new Job[count];
        index = 0;
        for (j = 0; j < jobIndexes.Length; j++) {
            if (jobIndexes[j] != -1) {
                jobs[i].InJobs[index] = jobs[jobIndexes[j]];
                index++;
            }
        }
        pathCount += count;
    }
    
    for (i = 0; i < jobs.Length; i++) {
        count = 0;
        for (j = 0; j < jobs.Length; j++) for (k = 0; k < jobs[j].InJobs.Length; k++) if (jobs[j].InJobs[k] == jobs[i]) count++;
        jobs[i].OutJobs = new Job[count];
        if (count == 0) continue;
        count = 0;
        for (j = 0; j < jobs.Length; j++) for (k = 0; k < jobs[j].InJobs.Length; k++) if (jobs[j].InJobs[k] == jobs[i]) jobs[i].OutJobs[count++] = jobs[j];
    }
    
    // Assign states to jobs
    for (i = 1; i < jobs.Length; i++) jobs[i].Status = -1;
    for (i = 1; i < JobStates.Length; i++) AssignJobState(i);
    for (i = 0; i < jobs.Length; i++) if (jobs[i].Status == -1) jobs[i].Status = WaitState;
    
    // Mark jobs that have started or are ready to start as ready
    bool ready;
    for (i = 1; i < jobs.Length; i++) {
        ready = (jobs[i].Status != WaitState);
        if (!ready) {
            ready = true;
            for (j = 0; j < jobs[i].InJobs.Length; j++) if (jobs[i].InJobs[j].Status != DoneState) ready = false;
        }
        jobs[i].Ready = ready;
    }
    
    // Assign columns to jobs
    bool done = false;
    while (!done) {
        done = true;
        for (i = 0; i < jobs.Length; i++) {
            if (jobs[i].InJobs.Length == 0) {
                jobs[i].Col = 0;
            } else {
                for (j = 0; j < jobs[i].InJobs.Length; j++) {
                    if (jobs[i].InJobs[j].Col != -1 && jobs[i].InJobs[j].Col >= jobs[i].Col) jobs[i].Col = jobs[i].InJobs[j].Col + 1;
                }
            }
            if (jobs[i].Col == -1) done = false;
        }
    }
    
    // Order jobs according to column
    Job js;
    for (i = 0; i < jobs.Length; i++) {
        next = i;
        for (j = i + 1; j < jobs.Length; j++) if (jobs[j].Col < jobs[next].Col) next = j;
        if (next != i) {
            js = jobs[next];
            for (j = next; j > i; j--) jobs[j] = jobs[j-1];
            jobs[i] = js;
        }
    }
    
    // Create array with column information
    if (jobs.Length > 0) cols = new Col[jobs[jobs.Length - 1].Col + 1];
    else cols = new Col[0];
    int newX = 0;
    index = 0;
    for (i = 0; i < cols.Length; i++) {
        cols[i] = new Col();
        cols[i].X = newX;
        cols[i].Width = BoxWidth;
        cols[i].FirstAvailableRow = 0;
        newX += BoxWidth + BoxToVertDistance + VertToArrowheadDistance + FilledArrowheadLength;
        cols[i].FirstJobIndex = index;
        while (index < jobs.Length && jobs[index].Col == i) {
            cols[i].JobCount++;
            cols[i].OutJobCount += jobs[index].OutJobs.Length;
            index++;
        }
        if (cols[i].JobCount > MaxJobsPerCol) MaxJobsPerCol = cols[i].JobCount;
        if (cols[i].OutJobCount > MaxOutJobsPerCol) MaxOutJobsPerCol = cols[i].OutJobCount;
    }
    
    // Create paths
    paths = new Path[pathCount];
    index = 0;
    for (i = 0; i < jobs.Length; i++) {
        for (j = 0; j < jobs[i].InJobs.Length; j++) {
            paths[index] = new Path();
            paths[index].StartJob = jobs[i].InJobs[j];
            paths[index].EndJob = jobs[i];
            paths[index].VertPathSegmentCol = -1;
            if (OutputFormat == "debug") Response.Write("Path " + paths[index].StartJob.Name + "->" + paths[index].EndJob.Name + "<br>"); 
            index++;
        }
    }
    
        // Set vertical positions of first column
    for (i = 0; i < jobs.Length /*&& jobs[i].Col == 0*/; i++) {
        jobs[i].Row = 2 * (i - cols[jobs[i].Col].FirstJobIndex) + MaxJobsPerCol - cols[jobs[i].Col].JobCount;
    }
    
    // (debug output)
    if (OutputFormat == "debug") {
        for (i = 0; i < jobs.Length; i++) {
            Response.Write("Job " + i + ": " + jobs[i].Name + " IN(");
            for (j = 0; j < jobs[i].InJobs.Length; j++) 
                if (jobs[i].InJobs[j] != null) Response.Write((j == 0 ? "" : " ") + jobs[i].InJobs[j].Name);
                else Response.Write((j == 0 ? "" : ",") + "-");
            Response.Write(") OUT(");
            for (j = 0; j < jobs[i].OutJobs.Length; j++) 
                if (jobs[i].OutJobs[j] != null) Response.Write((j == 0 ? "" : " ") + jobs[i].OutJobs[j].Name);
                else Response.Write((j == 0 ? "" : ",") + "-");
            Response.Write(") Col=" + jobs[i].Col + " Row=" + jobs[i].Row);
            Response.Write("<br>");
        }
    }
    
    // Optimize vertical positions of jobs in a column
    int iterations = 0;
    bool iterate = true;
    while (iterate && iterations < 2) {
        iterate = false;
        iterations++;
        for (i = 1; i < cols.Length; i++) {
            ArrangeJobsFromLeft(i, true);
            if (OutputFormat == "debug") {
                for (j = cols[i].FirstJobIndex; j < cols[i].FirstJobIndex + cols[i].JobCount; j++) {
                    Response.Write("-> Col " + i + " " + jobs[j].Name + ": Row=" + jobs[j].Row + "<br>");
                }
            }
            if (iterations > MaxDistributeIterations - 1 || i > 1) continue;
            
            ArrangeJobsFromRight(i - 1, cols[i-1].JobCount < cols[i].JobCount);
            if (cols[i-1].JobCount > cols[i].JobCount) {
                ArrangeJobsFromLeft(i - 1, false);
                ArrangeJobsFromLeft(i, true);
            }
        }
    }
    
    // For paths with vertical segments, try to put vertical segment just before the end job (only if there are no jobs between)
    bool conflict;
    for (i = 0; i < paths.Length; i++) {
        if (paths[i].EndJob.Row != paths[i].StartJob.Row) {
            paths[i].VertPathSegmentCol = paths[i].StartJob.Col;
            if (paths[i].EndJob.Col - paths[i].StartJob.Col > 1) {
                conflict = false;
                for (j = 0; j < jobs.Length; j++) {
                    if (jobs[j].Col > paths[i].StartJob.Col && jobs[j].Col < paths[i].EndJob.Col && (jobs[j].Row - paths[i].StartJob.Row) * (jobs[j].Row - paths[i].EndJob.Row) <= 0) {
                        conflict = true;
                        break;
                    }
                }
                if (OutputFormat == "debug") Response.Write("--- path " + paths[i].StartJob.Name + "," + paths[i].EndJob.Name + ": " + conflict + "<br>");
                if (!conflict) paths[i].VertPathSegmentCol = paths[i].VertPathSegmentCol = paths[i].EndJob.Col - 1;
            }
        }
    }
    
    // Shrink if between two columns are no vertical lines
    for (i = 0; i < cols.Length; i++) cols[i].HasVertPathSegments = false;
    for (i = 0; i < paths.Length; i++) if (paths[i].VertPathSegmentCol != -1/* && paths[i].EndJob.Col == paths[i].VertPathSegmentCol + 1*/) cols[paths[i].VertPathSegmentCol].HasVertPathSegments = true;
    for (i = 0; i < cols.Length; i++) if (!cols[i].HasVertPathSegments) {
        for (j = i + 1; j < cols.Length; j++) cols[j].X -= HorzShrink;
    }

    // Determine lowest and highest row indexes and shift all jobs down or up if the topmost job is not at row zero  
    for (i = 0; i < jobs.Length; i++) {
        if (i == 0 || jobs[i].Row < MinRow) MinRow = jobs[i].Row;
        if (i == 0 || jobs[i].Row > MaxRow) MaxRow = jobs[i].Row;
    }
    if (MinRow != 0) {
        for (i = 0; i < jobs.Length; i++) jobs[i].Row -= MinRow;
        MaxRow -= MinRow;
    }
    
        
    // Calculate image width and height
    CalculateImageBoundings();

    GridRowHeight = (BoxHeight + BoxVertSpacing) / 2;
    BoxHalfWidth = BoxWidth / 2; 
    LabelXOffset = BoxWidth / 2; 
    PathYOffset = BoxHeight / 2;
    
    // (debug output)
    if (OutputFormat == "debug") {
        Response.Write("<br>");
        for (i = 0; i < jobs.Length; i++) {
            Response.Write("Job " + i + ": " + jobs[i].Name + " IN(");
            for (j = 0; j < jobs[i].InJobs.Length; j++) 
                if (jobs[i].InJobs[j] != null) Response.Write((j == 0 ? "" : " ") + jobs[i].InJobs[j].Name);
                else Response.Write((j == 0 ? "" : ",") + "-");
            Response.Write(") OUT(");
            for (j = 0; j < jobs[i].OutJobs.Length; j++) 
                if (jobs[i].OutJobs[j] != null) Response.Write((j == 0 ? "" : " ") + jobs[i].OutJobs[j].Name);
                else Response.Write((j == 0 ? "" : ",") + "-");
            Response.Write(") Col=" + jobs[i].Col + " Row=" + jobs[i].Row);
            Response.Write("<br>");
        }
    
        for (i = 0; i < cols.Length; i++) {
            Response.Write("Col " + i + ": " + cols[i].X + " first=" + cols[i].FirstJobIndex + " count=" + cols[i].JobCount + " outjobs=" + cols[i].OutJobCount);
            Response.Write("<br>");
        }
        Response.Write("MaxJobsPerCol = " + MaxJobsPerCol + "<br>");
        return;
    }
    
    if (OutputFormat == "svg") RenderSvgImage();
    else if (OutputFormat == "png") RenderPngImage();
}


/*****************************************************************************************************************************/
void ArrangeJobsFromLeft(int col, bool Calculate) {
    int sum, diff, shiftUp, shiftDown, next, i, j;
    Job js;
    if (Calculate) {
        for (i = cols[col].FirstJobIndex; i < cols[col].FirstJobIndex + cols[col].JobCount; i++) {
            sum = 0;
            for (j = 0; j < jobs[i].InJobs.Length; j++) sum += jobs[i].InJobs[j].Row;
            jobs[i].TempRow = (sum + jobs[i].InJobs.Length - 1) / jobs[i].InJobs.Length;
        }
    }
    for (i = cols[col].FirstJobIndex; i < cols[col].FirstJobIndex + cols[col].JobCount; i++) jobs[i].Row = jobs[i].TempRow;
    // Order jobs within column (first criterion: new row, second criterion: original job index)
    for (i = cols[col].FirstJobIndex; i < cols[col].FirstJobIndex + cols[col].JobCount; i++) {
        next = i;
        for (j = i + 1; j < cols[col].FirstJobIndex + cols[col].JobCount; j++) {
            if (jobs[j].Row < jobs[i].Row) next = j;
        }
        if (next != i) {
            js = jobs[next];
            for (j = next; j > i; j--) jobs[j] = jobs[j-1];
            jobs[i] = js;
        }
    }
    // Shift jobs occupying the same space up or down (with least impact on other jobs of the same column)
    for (i = cols[col].FirstJobIndex; i < cols[col].FirstJobIndex + cols[col].JobCount - 1; i++) {
        if (jobs[i+1].Row - jobs[i].Row < 2) {
            shiftUp = 0;
            shiftDown = 0;
            for (j = cols[col].FirstJobIndex; j <= i; j++) {
                jobs[j].TempRow = 2 * (j - i) - 2 + jobs[i+1].Row;
                shiftUp += (jobs[j].Row > jobs[j].TempRow ? jobs[j].Row - jobs[j].TempRow : 0);
            }
            for (j = i + 1; j < cols[col].FirstJobIndex + cols[col].JobCount; j++) {
                jobs[j].TempRow = 2 * (j - i) + jobs[i].Row;
                shiftDown += (jobs[j].Row < jobs[j].TempRow ? jobs[j].TempRow - jobs[j].Row : 0);
            }
            if (OutputFormat == "debug") Response.Write("col=" + col + ", job=" + jobs[i].Name + "::row=" + jobs[i].Row + " up:" + shiftUp + " - down:" + shiftDown);
            if (shiftUp < shiftDown) {
                for (j = cols[col].FirstJobIndex; j <= i; j++) if (jobs[j].Row > jobs[j].TempRow) jobs[j].Row = jobs[j].TempRow;
            } else if (shiftDown < shiftUp) {
                for (j = i + 1; j < cols[col].FirstJobIndex + cols[col].JobCount; j++) if (jobs[j].Row < jobs[j].TempRow) jobs[j].Row = jobs[j].TempRow;
            } else {
                for (j = cols[col].FirstJobIndex; j <= i; j++) {
                    jobs[j].TempRow = 2 * (j - i) - 1 + jobs[i+1].Row;
                    if (jobs[j].Row > jobs[j].TempRow) jobs[j].Row = jobs[j].TempRow;
                }
                for (j = i + 1; j < cols[col].FirstJobIndex + cols[col].JobCount; j++) {
                    jobs[j].TempRow = 2 * (j - i) + jobs[i].Row;
                    if (jobs[j].Row < jobs[j].TempRow) jobs[j].Row = jobs[j].TempRow;
                }
            }
            if (OutputFormat == "debug") Response.Write(" -> row=" + jobs[i].Row + "<br>");
        }
    }
}

/*****************************************************************************************************************************/
void ArrangeJobsFromRight(int col, bool Distribute) {
    int sum, diff, i, j;
    for (i = cols[col].FirstJobIndex; i < cols[col].FirstJobIndex + cols[col].JobCount; i++) {
        if (jobs[i].OutJobs.Length == 0) continue;
        sum = 0;
        for (j = 0; j < jobs[i].OutJobs.Length; j++) sum += jobs[i].OutJobs[j].Row;
        jobs[i].TempRow = (sum + jobs[i].OutJobs.Length - 1) / jobs[i].OutJobs.Length;
        if (OutputFormat == "debug") Response.Write("* " + jobs[i].Name + " oldrow=" + jobs[i].Row + " newrow=" + jobs[i].TempRow + "<br>");
    }
    if (Distribute) {
        for (i = cols[col].FirstJobIndex + 1; i < cols[col].FirstJobIndex + cols[col].JobCount; i++) {
            diff = (jobs[i].Row - jobs[i-1].Row) - (jobs[i].TempRow - jobs[i-1].TempRow);
            if (diff > 0) {
                //iterate = true;
                for (j = i; j < cols[col].FirstJobIndex + cols[col].JobCount; j++) jobs[j].TempRow += diff;
                if (OutputFormat == "debug") Response.Write("diff=" + diff + "<br>");
            }
        }
        for (i = cols[col].FirstJobIndex; i < cols[col].FirstJobIndex + cols[col].JobCount; i++) {
            jobs[i].Row = jobs[i].TempRow;
            if (OutputFormat == "debug") Response.Write("*** " + jobs[i].Name + " newrow=" + jobs[i].TempRow + " row=" + jobs[i].Row + "<br>");
        }
    }
}

/*****************************************************************************************************************************/
void CalculateImageBoundings() {
    ImageWidth = 2 * HorzPadding;
    ImageHeight = 2 * VertPadding;
    if (cols.Length > 0) {
        ImageWidth += cols[cols.Length - 1].X + cols[cols.Length - 1].Width;
        ImageHeight += ((BoxHeight + BoxVertSpacing) / 2) * MaxRow + BoxHeight;
    }
    DisplayWidth = ImageWidth;
    DisplayHeight = ImageHeight;
    int ZoomFactor = GetUrlParameterInt("zoom", 0);
    int MinWidth = GetUrlParameterInt("minwidth", 0);
    int MinHeight = GetUrlParameterInt("minheight", 0);
    int MaxWidth = GetUrlParameterInt("maxwidth", 0);
    int MaxHeight = GetUrlParameterInt("maxheight", 0);
    if (ZoomFactor > 0) {
        DisplayWidth = (ImageWidth * ZoomFactor) / 100;
        DisplayHeight = (ImageHeight * ZoomFactor) / 100;
    } else if ((MinWidth > 0 && ImageWidth < MinWidth) || (MinHeight > 0 && ImageHeight < MinHeight)) {
        if (MinWidth > 0 && DisplayWidth < MinWidth) {
            DisplayWidth = MinWidth;
            DisplayHeight = (ImageHeight * MinWidth) / ImageWidth;
        }
        if (MinHeight > 0 && DisplayHeight < MinHeight) {
            DisplayWidth = (ImageWidth * MinHeight) / ImageHeight;
            DisplayHeight = MinHeight;
        }
    } else if ((MaxWidth > 0 && ImageWidth > MaxWidth) || (MaxHeight > 0 && ImageHeight > MaxHeight)) {
        if (MaxWidth > 0 && DisplayWidth > MaxWidth) {
            DisplayWidth = MaxWidth;
            DisplayHeight = (ImageHeight * MaxWidth) / ImageWidth;
        }
        if (MaxHeight > 0 && DisplayHeight > MaxHeight) {
            DisplayWidth = (ImageWidth * MaxHeight) / ImageHeight;
            DisplayHeight = MaxHeight;
        }
    }
    
    if (OutputFormat == "png" && DisplayWidth != ImageWidth) {
        for (i = 0; i < cols.Length; i++) {
            cols[i].X = (cols[i].X * DisplayWidth) / ImageWidth;
            cols[i].Width = (cols[i].Width * DisplayWidth) / ImageWidth;
        }
        JobFontSize = (JobFontSize * DisplayWidth) / ImageWidth;
        LabelYOffset = (LabelYOffset * DisplayWidth) / ImageWidth;
        VertPadding = (VertPadding * DisplayWidth) / ImageWidth;
        HorzPadding = (HorzPadding * DisplayWidth) / ImageWidth;
        BoxWidth = (BoxWidth * DisplayWidth) / ImageWidth;
        BoxHeight = (BoxHeight * DisplayWidth) / ImageWidth;
        BoxCornerRadius = (BoxCornerRadius * DisplayWidth) / ImageWidth;
        BoxVertSpacing = (BoxVertSpacing * DisplayWidth) / ImageWidth;
        CurveRadius = (CurveRadius * DisplayWidth) / ImageWidth;
        HorzShrink = (HorzShrink * DisplayWidth) / ImageWidth;
        BoxToVertDistance = (BoxToVertDistance * DisplayWidth) / ImageWidth + 1;
        VertToArrowheadDistance = (VertToArrowheadDistance * DisplayWidth) / ImageWidth - 1;
        PathStrokeWidth = (PathStrokeWidth * DisplayWidth) / ImageWidth;
        VertPathsDistance = (VertPathsDistance * DisplayWidth) / ImageWidth;
        FilledArrowheadHalfWidth = (FilledArrowheadHalfWidth * DisplayWidth) / ImageWidth;
        FilledArrowheadLength = (FilledArrowheadLength * DisplayWidth) / ImageWidth;
        StrokedArrowheadHalfWidth = (StrokedArrowheadHalfWidth * DisplayWidth) / ImageWidth;
        StrokedArrowheadLength = (StrokedArrowheadLength * DisplayWidth) / ImageWidth;
        StrokedArrowheadStrokeWidth = (StrokedArrowheadStrokeWidth * DisplayWidth) / ImageWidth;
    }
}

/*****************************************************************************************************************************/
void RenderSvgImage() {
    string BoxAttributes = "width=\"" + BoxWidth + "\" height=\"" + BoxHeight + "\"";
    if (BoxCornerRadius != 0) BoxAttributes += " rx=\"" + BoxCornerRadius + "\" ry=\"" + BoxCornerRadius + "\"";
    string PathCurveES = "", PathCurveEN = "", PathCurveSE = "", PathCurveNE = "";
    if (CurveRadius > 0) {
        PathCurveES = " a" + CurveRadius + "," + CurveRadius + " 0 0,1 " + CurveRadius + "," + CurveRadius;
        PathCurveEN = " a" + CurveRadius + "," + CurveRadius + " 0 0,0 " + CurveRadius + ",-" + CurveRadius;
        PathCurveSE = " a" + CurveRadius + "," + CurveRadius + " 0 0,0 " + CurveRadius + "," + CurveRadius;
        PathCurveNE = " a" + CurveRadius + "," + CurveRadius + " 0 0,1 " + CurveRadius + ",-" + CurveRadius;
    }
    
    string FilledPathArrowhead = "l" + (-FilledArrowheadLength) + "," + (-FilledArrowheadHalfWidth) + " v" + (2 * FilledArrowheadHalfWidth) + " l" + FilledArrowheadLength + "," + (-FilledArrowheadHalfWidth);
    string StrokedPathArrowhead = "l" + (-StrokedArrowheadLength) + "," + (-StrokedArrowheadHalfWidth) + " v" + (2 * StrokedArrowheadHalfWidth) + " l" + StrokedArrowheadLength + "," + (-StrokedArrowheadHalfWidth);

    SetContentType("image/svg+xml");

    Response.Write("<?xml version=\"1.0\" standalone=\"no\"?>\n");
    Response.Write("<!DOCTYPE svg PUBLIC \"-//W3C//DTD SVG 1.1//EN\" \"http://www.w3.org/Graphics/SVG/1.1/DTD/svg11.dtd\">\n");
    Response.Write("<svg width=\"" + DisplayWidth + "\" height=\"" + DisplayHeight + "\" viewBox=\"0 0 " + ImageWidth + " " + ImageHeight + "\" version=\"1.1\" xmlns=\"http://www.w3.org/2000/svg\">\n");
    Response.Write("	<style type=\"text/css\"><![CDATA[\n");
    for (i = 0; i < JobStates.Length; i++) {
        Response.Write("		." + JobStates[i] + " { ");
        if (JobStateStrokeWidths[i] != 0) {
            Response.Write("stroke:#" + JobStateStrokeColors[i].ToString("X6") + "; ");
            Response.Write("stroke-width:" + JobStateStrokeWidths[i] + "; ");
        } else {
            Response.Write("stroke:none; ");
        }
        Response.Write("fill:#" + JobStateFillColors[i].ToString("X6") + "; }\n");
    }
    Response.Write("		.path { stroke:#" + PathColor.ToString("X6") + "; stroke-width:" + PathStrokeWidth + "; fill:none; }\n");
    if (DisambiguatePaths) {
        Response.Write("		.path_white { stroke:#ffffff; stroke-width:" + (PathStrokeWidth + 2) + "; fill:none; }\n");
    }
    Response.Write("		.arrow { stroke:none; fill:#" + PathColor.ToString("X6") + "; }\n");
    if (DifferentArrows) {
        Response.Write("		.arrow_wait { stroke:#" + PathColor.ToString("X6") + "; stroke-width:" + StrokedArrowheadStrokeWidth + "; fill:none; }\n");
    }
    Response.Write("		.label { font-family:" + JobFontFamily + "; font-size:" + JobFontSize + "pt; stroke:none; fill:#" + JobFontColor.ToString("X6") + "; }\n");
    Response.Write("	]]></style>\n");
    
    // Paths
    for (i = 0; i < paths.Length; i++) {
        StartX = cols[paths[i].StartJob.Col].X + cols[paths[i].StartJob.Col].Width + HorzPadding;
        StartY = paths[i].StartJob.Row * GridRowHeight + PathYOffset + VertPadding;
        EndX = cols[paths[i].EndJob.Col].X - (paths[i].EndJob.Ready ? FilledArrowheadLength : StrokedArrowheadLength) + HorzPadding;
        EndY = paths[i].EndJob.Row * GridRowHeight + PathYOffset + VertPadding;

        if (paths[i].EndJob.Row == paths[i].StartJob.Row) {
            Response.Write("	<path class=\"path\" d=\"M" + StartX + "," + StartY + " h" + (EndX - StartX) + "\" />\n");
        } else {
            VertSegmentX = cols[paths[i].VertPathSegmentCol].X + cols[paths[i].VertPathSegmentCol].Width + BoxToVertDistance + HorzPadding;
            Response.Write("	<path class=\"path\" d=\"");
            Response.Write("M" + StartX + "," + StartY);
            Response.Write(" h" + (VertSegmentX - StartX - CurveRadius));
            if (paths[i].EndJob.Row < paths[i].StartJob.Row) {
                Response.Write(PathCurveEN);
                Response.Write(" v" + (EndY - StartY + 2 * CurveRadius));
                Response.Write(PathCurveNE);
            } else {
                Response.Write(PathCurveES);
                Response.Write(" v" + (EndY - StartY - 2 * CurveRadius));
                Response.Write(PathCurveSE);
            }
            Response.Write(" h" + (EndX - VertSegmentX - CurveRadius));
            Response.Write("\" />\n");
        }
    }
        
    // Arrowheads
    for (i = 0; i < jobs.Length; i++) {
        if (jobs[i].InJobs.Length == 0) continue;
        if (!DifferentArrows || jobs[i].Ready) Response.Write("	<path class=\"arrow\" d=\"M" + (cols[jobs[i].Col].X + HorzPadding) + "," + (jobs[i].Row * GridRowHeight + PathYOffset + VertPadding) + " " + FilledPathArrowhead + "\" />\n");
        else Response.Write("	<path class=\"arrow_wait\" d=\"M" + (cols[jobs[i].Col].X + HorzPadding) + "," + (jobs[i].Row * GridRowHeight + PathYOffset + VertPadding) + " " + StrokedPathArrowhead + "\" />\n");
    }
    
    // Boxes and labels
    for (i = 0; i < jobs.Length; i++) {
        Response.Write("	<rect class=\"" + JobStates[jobs[i].Status] + "\" x=\""  + (cols[jobs[i].Col].X + HorzPadding) + "\" y=\"" + (jobs[i].Row * GridRowHeight + VertPadding) + "\" " + BoxAttributes + " />\n");
        Response.Write("	<text class=\"label\" x=\"" + (cols[jobs[i].Col].X + LabelXOffset + HorzPadding) + "\" y=\"" + (jobs[i].Row * GridRowHeight + PathYOffset + LabelYOffset + VertPadding) + "\" style=\"text-anchor:middle\">" + jobs[i].Name + "</text>\n");
    }

    Response.Write("</svg>\n");
}

/*****************************************************************************************************************************/
void RenderPngImage() {
    Bitmap bmp = new Bitmap(DisplayWidth, DisplayHeight);
    Graphics g = Graphics.FromImage(bmp);
    if (ShapeSmoothing) g.SmoothingMode = SmoothingMode.AntiAlias;
    if (FontSmoothing) g.TextRenderingHint = TextRenderingHint.AntiAlias; 
    SolidBrush brush = new SolidBrush(Color.Black);
    Pen pen = new Pen(Color.FromArgb(PathColor | -0x01000000), (PathStrokeWidth > 1 ? PathStrokeWidth - 0.49F : PathStrokeWidth));
    GraphicsPath path = new GraphicsPath();
    Font font = new Font(JobFontFamily, JobFontSize);
    StringFormat labelFormat = new StringFormat();
    labelFormat.Alignment = StringAlignment.Center;
    labelFormat.LineAlignment = StringAlignment.Center;
    
    SetContentType("image/png");
    
    // Paths
    for (i = 0; i < paths.Length; i++) {
        StartX = cols[paths[i].StartJob.Col].X + cols[paths[i].StartJob.Col].Width + HorzPadding;
        StartY = paths[i].StartJob.Row * GridRowHeight + PathYOffset + VertPadding;
        EndX = cols[paths[i].EndJob.Col].X - (paths[i].EndJob.Ready ? FilledArrowheadLength : StrokedArrowheadLength) + HorzPadding;
        EndY = paths[i].EndJob.Row * GridRowHeight + PathYOffset + VertPadding;
        
        path.Reset();
        path.StartFigure();
        if (paths[i].EndJob.Row == paths[i].StartJob.Row) {
            path.AddLine(StartX, StartY, EndX, EndY);
        } else {
            VertSegmentX = cols[paths[i].VertPathSegmentCol].X + cols[paths[i].VertPathSegmentCol].Width + BoxToVertDistance + HorzPadding;
            path.AddLine(StartX, StartY, VertSegmentX - CurveRadius, StartY);
            if (paths[i].EndJob.Row < paths[i].StartJob.Row) {
                if (CurveRadius > 0) path.AddArc(VertSegmentX - 2 * CurveRadius, StartY - 2 * CurveRadius, 2 * CurveRadius, 2 * CurveRadius, 90, -90);
                path.AddLine(VertSegmentX, StartY - CurveRadius, VertSegmentX, EndY + CurveRadius); 
                if (CurveRadius > 0) path.AddArc(VertSegmentX, EndY, 2 * CurveRadius, 2 * CurveRadius, 180, 90);
            } else {
                if (CurveRadius > 0) path.AddArc(VertSegmentX - 2 * CurveRadius, StartY, 2 * CurveRadius, 2 * CurveRadius, 270, 90);
                path.AddLine(VertSegmentX, StartY + CurveRadius, VertSegmentX, EndY - CurveRadius); 
                if (CurveRadius > 0) path.AddArc(VertSegmentX, EndY - 2 * CurveRadius, 2 * CurveRadius, 2 * CurveRadius, 180, -90);
            }
            path.AddLine(VertSegmentX + CurveRadius, EndY, EndX, EndY);
        }
        g.DrawPath(pen, path);
    }
    
    // Arrowheads
    for (i = 0; i < jobs.Length; i++) {
        EndX = cols[jobs[i].Col].X + HorzPadding;
        EndY = jobs[i].Row * GridRowHeight + PathYOffset + VertPadding;
        path.Reset();
        path.StartFigure();
        brush.Color = Color.FromArgb(PathColor | -0x01000000);
        if (jobs[i].InJobs.Length == 0) continue;
        if (!DifferentArrows || jobs[i].Ready) {
            path.AddLine(EndX, EndY, EndX - FilledArrowheadLength, EndY - FilledArrowheadHalfWidth); 
            path.AddLine(EndX - FilledArrowheadLength, EndY - FilledArrowheadHalfWidth, EndX - FilledArrowheadLength, EndY + FilledArrowheadHalfWidth); 
            path.AddLine(EndX - FilledArrowheadLength, EndY + FilledArrowheadHalfWidth, EndX, EndY); 
            g.FillPath(brush, path);
        }
        else {
            brush.Color = Color.Transparent;
            path.AddLine(EndX, EndY, EndX - StrokedArrowheadLength, EndY - StrokedArrowheadHalfWidth); 
            path.AddLine(EndX - StrokedArrowheadLength, EndY - StrokedArrowheadHalfWidth, EndX - StrokedArrowheadLength, EndY + StrokedArrowheadHalfWidth); 
            path.AddLine(EndX - StrokedArrowheadLength, EndY + StrokedArrowheadHalfWidth, EndX, EndY); 
            g.DrawPath(pen, path);
        }
    }

    // Boxes and labels
    for (i = 0; i < jobs.Length; i++) {
        StartX = cols[jobs[i].Col].X + HorzPadding + 1;
        StartY = jobs[i].Row * GridRowHeight + VertPadding;
        brush.Color = Color.FromArgb(JobStateFillColors[jobs[i].Status] | -0x01000000); 
        if (BoxCornerRadius == 0) {
            g.FillRectangle(brush, StartX, StartY, BoxWidth, BoxHeight);
        } else if (CurveRadius < BoxHalfWidth) {
            path.Reset();
            path.StartFigure();
            path.AddLine(StartX + BoxCornerRadius, StartY, StartX + BoxWidth - BoxCornerRadius, StartY);
            if (BoxCornerRadius > 0) path.AddArc(StartX + BoxWidth - 2 * BoxCornerRadius, StartY, 2 * BoxCornerRadius, 2 * BoxCornerRadius, 270, 90);
            path.AddLine(StartX + BoxWidth, StartY + BoxCornerRadius, StartX + BoxWidth, StartY + BoxHeight - BoxCornerRadius);
            if (BoxCornerRadius > 0) path.AddArc(StartX + BoxWidth - 2 * BoxCornerRadius, StartY + BoxHeight - 2 * BoxCornerRadius, 2 * BoxCornerRadius, 2 * BoxCornerRadius, 0, 90);
            path.AddLine(StartX + BoxWidth - BoxCornerRadius, StartY + BoxHeight, StartX + BoxCornerRadius, StartY + BoxHeight);
            if (BoxCornerRadius > 0) path.AddArc(StartX, StartY + BoxHeight - 2 * BoxCornerRadius, 2 * BoxCornerRadius, 2 * BoxCornerRadius, 90, 90);
            path.AddLine(StartX, StartY + BoxHeight - BoxCornerRadius, StartX, StartY + BoxCornerRadius);
            if (BoxCornerRadius > 0) path.AddArc(StartX, StartY, 2 * BoxCornerRadius, 2 * BoxCornerRadius, 180, 90);
            g.FillPath(brush, path);
        } else {
            g.FillEllipse(brush, StartX, StartY, BoxWidth, BoxHeight);
        }
        
        brush.Color = Color.White;
        labelFormat.Alignment = StringAlignment.Center;
        brush.Color = Color.FromArgb(JobFontColor | -0x01000000);
        g.DrawString(jobs[i].Name, font, brush, new RectangleF(StartX, StartY, BoxWidth, BoxHeight), labelFormat);
    }
    
    MemoryStream memStream = new MemoryStream(); // bitmap cannot be saved directly into output stream
    bmp.Save(memStream, ImageFormat.Png);        // see http://www.west-wind.com/WebLog/posts/8230.aspx
    memStream.WriteTo(Response.OutputStream);
    
    g.Dispose();
    bmp.Dispose();

    Response.Flush();
}

/*****************************************************************************************************************************/
void AssignJobState(int index) {
    string jobName;
    for (int i = 0; i < GetUrlParameter(JobStates[index], "", false).Split(';').Length; i++) {
        jobName = GetUrlParameter(JobStates[index], "", false).Split(';')[i];
        for (int j = 0; j < jobs.Length; j++) if (jobs[j].Name == jobName) jobs[j].Status = index;
    }
}

/*****************************************************************************************************************************/
string GetUrlParameter(string name, string defaultValue, bool lowerCase) {
    string result = Request.QueryString[name];
    if (result == null || result == "") return defaultValue;
    if (lowerCase) result = result.ToLower();
    return result;
}

/*****************************************************************************************************************************/
int GetUrlParameterInt(string name, int defaultValue) {
    string s = Request.QueryString[name];
    if (s == null || s == "") return defaultValue;
    try {
        return Int32.Parse(s);
    } catch (Exception) {
        return defaultValue;
    }
}

/*****************************************************************************************************************************/
void SetContentType(string contentType) {
    Response.ContentType = contentType;
    if (contentType.EndsWith("xml")) Response.ContentEncoding = Encoding.UTF8;
}

/*****************************************************************************************************************************/
class Job {
    public string Name;                 // Job name, if empty, job is considered as "virtual" job for graphical representation
    public int Status;                  // Status (index of status in array JobStates)
    public Job[] InJobs;                // List of indexes of incoming jobs
    public Job[] OutJobs;               // List of indexes of outgoing jobs
    public bool Ready;                  // True if all input jobs are finished or if job is running
    public int Col, Row;                // X and Y position in resulting grid (Row difference must be at least 2 for jobs with the same Col value)
    public int TempRow;                 // For temporary use: row
    public int ColInJobCount;           // For temporary use: job depends directly on a job within a certain column
    public int PathCrossCount;
}

class Col {
    public bool Virtual;
    public int X, Width;
    public int FirstJobIndex, JobCount, OutJobCount;
    public int FirstAvailableRow;
    public bool HasVertPathSegments;
}

class Path {
    public Job StartJob, EndJob;
    public int VertPathSegmentCol;
}

struct CrossingPathInfo {
    public int CrossCount;              // Number of crossing paths
    public int JobCount;                // Number of jobs with that is start or end of at least one crossing path
    public int JobChangePairCount;
    public ChangePair[] JobChangePairs;            // Indexes of jobs that can be changed

    public CrossingPathInfo(int MaxOutJobsPerCol) {
        CrossCount = 0;
        JobCount = 0;
        JobChangePairCount = 0;
        JobChangePairs = new ChangePair[MaxOutJobsPerCol * (MaxOutJobsPerCol - 1)];  
    }
}

struct ChangePair {
    public int idx1, idx2;
}

</script>

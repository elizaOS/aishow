#Requires -Version 5.1
<#
.SYNOPSIS
    Batch converts video files using FFmpeg to a Unity-friendly format.
.DESCRIPTION
    This script iterates through video files (e.g., .mp4, .mov) in a specified input directory,
    converts them using FFmpeg with settings aimed at resolving color space issues and ensuring
    compatibility with Unity, and saves the converted files to a specified output directory.

    Before running:
    1. Ensure FFmpeg is installed and its directory is added to your system's PATH,
       or provide the full path to ffmpeg.exe in the script.
    2. Create the input and output directories if they don't exist.
    3. Back up your original videos.
.PARAMETER InputDirectory
    The path to the directory containing the video files to convert.
.PARAMETER OutputDirectory
    The path to the directory where converted video files will be saved.
    If not specified, an 'ConvertedVideos' subdirectory will be created in the InputDirectory.
.PARAMETER FfmpegPath
    The full path to ffmpeg.exe. If FFmpeg is in your system PATH, this can be just "ffmpeg.exe".
    Default is "ffmpeg.exe".
.PARAMETER VideoExtensions
    An array of video file extensions to process. Default is @("*.mp4", "*.mov").
.EXAMPLE
    .\Convert-Videos.ps1 -InputDirectory "C:\Users\dev\VideosToConvert" -OutputDirectory "C:\Users\dev\ConvertedVideos"

    This command converts all .mp4 and .mov files from "C:\Users\dev\VideosToConvert"
    and saves them to "C:\Users\dev\ConvertedVideos".
.EXAMPLE
    .\Convert-Videos.ps1 -InputDirectory "C:\Path\To\Your\Videos"

    Converts videos in the specified path and saves them to a subdirectory named 'ConvertedVideos'
    within "C:\Path\To\Your\Videos".
#>
param (
    [Parameter(Mandatory=$true)]
    [string]$InputDirectory,

    [Parameter(Mandatory=$false)]
    [string]$OutputDirectory,

    [Parameter(Mandatory=$false)]
    [string]$FfmpegPath = "ffmpeg.exe",

    [Parameter(Mandatory=$false)]
    [string[]]$VideoExtensions = @("*.mp4", "*.mov", "*.avi", "*.webm") # Added more common extensions
)

# Validate Input Directory
if (-not (Test-Path -Path $InputDirectory -PathType Container)) {
    Write-Error "Input directory '$InputDirectory' not found."
    exit 1
}

# Set Output Directory if not specified
if ([string]::IsNullOrEmpty($OutputDirectory)) {
    $OutputDirectory = Join-Path -Path $InputDirectory -ChildPath "ConvertedVideos"
}

# Create Output Directory if it doesn't exist
if (-not (Test-Path -Path $OutputDirectory -PathType Container)) {
    try {
        New-Item -ItemType Directory -Path $OutputDirectory -Force -ErrorAction Stop | Out-Null
        Write-Host "Created output directory: $OutputDirectory"
    }
    catch {
        Write-Error "Failed to create output directory '$OutputDirectory'. Error: $($_.Exception.Message)"
        exit 1
    }
}

Write-Host "Starting video conversion..."
Write-Host "Input Directory: $InputDirectory"
Write-Host "Output Directory: $OutputDirectory"
Write-Host "FFmpeg Path: $FfmpegPath"
Write-Host "Processing extensions: $($VideoExtensions -join ', ')"

# Get video files
$videoFiles = Get-ChildItem -Path $InputDirectory -Recurse -Include $VideoExtensions

if ($videoFiles.Count -eq 0) {
    Write-Warning "No video files found in '$InputDirectory' with the specified extensions."
    exit 0
}

Write-Host "Found $($videoFiles.Count) video file(s) to process."

foreach ($videoFile in $videoFiles) {
    $inputFilePath = $videoFile.FullName
    $fileNameWithoutExtension = $videoFile.BaseName
    $fileExtension = $videoFile.Extension # e.g. .mp4
    
    # Maintain the original extension for the output file, or you can force .mp4
    $outputFileName = "${fileNameWithoutExtension}_converted${fileExtension}"
    # If you want all output to be .mp4, uncomment the line below and comment the one above
    # $outputFileName = "${fileNameWithoutExtension}_converted.mp4"
    
    $outputFilePath = Join-Path -Path $OutputDirectory -ChildPath $outputFileName

    Write-Host "Processing: $inputFilePath"

    # FFmpeg command arguments
    # -y: Overwrite output files without asking
    # -i: Input file
    # -c:v libx264: Video codec H.264
    # -pix_fmt yuv420p: Pixel format for wide compatibility
    # -c:a aac: Audio codec AAC
    # -b:a 192k: Audio bitrate (optional, adjust as needed)
    # -preset medium: Encoding speed/compression trade-off (ultrafast, superfast, veryfast, faster, fast, medium, slow, slower, veryslow)
    # -crf 23: Constant Rate Factor for H.264 (0-51, lower is better quality, 18-28 is a sane range. 23 is default for medium)
    $ffmpegArgs = "-y -i `"$inputFilePath`" -c:v libx264 -pix_fmt yuv420p -preset medium -crf 23 -c:a aac -b:a 192k `"$outputFilePath`""

    try {
        Write-Host "Executing: $FfmpegPath $ffmpegArgs"
        # Start FFmpeg process
        $process = Start-Process -FilePath $FfmpegPath -ArgumentList $ffmpegArgs -Wait -NoNewWindow -PassThru -ErrorAction Stop

        if ($process.ExitCode -eq 0) {
            Write-Host "Successfully converted: $inputFilePath to $outputFilePath" -ForegroundColor Green
        } else {
            Write-Error "FFmpeg conversion failed for '$inputFilePath'. Exit code: $($process.ExitCode)"
            # Consider logging FFmpeg output here if needed for detailed errors
        }
    }
    catch {
        Write-Error "An error occurred while trying to run FFmpeg for '$inputFilePath'. Error: $($_.Exception.Message)"
        # Check if FFmpeg is found
        if ($_.Exception.Message -like "*The system cannot find the file specified*") {
             Write-Warning "Ensure FFmpeg is installed and '$FfmpegPath' is correct (or FFmpeg is in your system PATH)."
        }
    }
}

Write-Host "Video conversion process completed."
Write-Host "Converted files are located in: $OutputDirectory" 
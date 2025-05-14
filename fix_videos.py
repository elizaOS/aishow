import os
import ffmpeg
import glob

def fix_video(input_path, output_path):
    try:
        # Set up the FFmpeg command with proper color primaries
        stream = ffmpeg.input(input_path)
        stream = ffmpeg.output(stream, output_path,
                             vcodec='libx264',
                             acodec='copy',
                             color_primaries='bt709',
                             color_trc='bt709',
                             colorspace='bt709')
        
        # Run the conversion
        ffmpeg.run(stream, overwrite_output=True)
        print(f"Successfully processed: {input_path}")
        return True
    except ffmpeg.Error as e:
        print(f"Error processing {input_path}: {str(e)}")
        return False

def main():
    # Get all video files in the Assets/Videos directory and its subdirectories
    video_extensions = ['*.mp4', '*.mov', '*.avi']
    video_files = []
    
    for ext in video_extensions:
        video_files.extend(glob.glob(os.path.join('Assets', 'Videos', '**', ext), recursive=True))
    
    if not video_files:
        print("No video files found in Assets/Videos directory")
        return
    
    print(f"Found {len(video_files)} video files to process")
    
    # Process each video
    for video_path in video_files:
        # Create output path in a 'fixed' subdirectory
        dir_name = os.path.dirname(video_path)
        file_name = os.path.basename(video_path)
        output_dir = os.path.join(dir_name, 'fixed')
        
        # Create output directory if it doesn't exist
        os.makedirs(output_dir, exist_ok=True)
        
        output_path = os.path.join(output_dir, file_name)
        
        # Skip if output file already exists
        if os.path.exists(output_path):
            print(f"Skipping {video_path} - already processed")
            continue
        
        print(f"Processing: {video_path}")
        fix_video(video_path, output_path)

if __name__ == "__main__":
    main() 
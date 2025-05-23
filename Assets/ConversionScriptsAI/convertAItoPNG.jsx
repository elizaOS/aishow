// Adobe Illustrator Script: Convert AI files to PNG with transparency
// Save this as a .jsx file and run it from Illustrator

function collectAIFilesRecursively(folder, aiFilesArray) {
    var items = folder.getFiles(); // Get all items (files and folders)
    for (var i = 0; i < items.length; i++) {
        var item = items[i];
        // Skip files starting with '._' and ensure it's an AI file
        if (item instanceof File && item.name.match(/\.ai$/i) && item.name.substring(0, 2) !== "._") {
            aiFilesArray.push(item);
        } else if (item instanceof Folder) {
            collectAIFilesRecursively(item, aiFilesArray); // Recursive call for subfolders
        }
    }
}

function convertAItoPNG() {
    // Get the folder containing AI files
    var sourceFolder = Folder.selectDialog("Select folder containing AI files:");
    if (!sourceFolder) {
        alert("No folder selected. Script cancelled.");
        return;
    }
    
    // Get the output folder
    var outputFolder = Folder.selectDialog("Select output folder for PNG files:");
    if (!outputFolder) {
        alert("No output folder selected. Script cancelled.");
        return;
    }
    
    // Get all AI files in the source folder and its subfolders
    var aiFiles = [];
    collectAIFilesRecursively(sourceFolder, aiFiles);
    
    if (aiFiles.length === 0) {
        alert("No AI files found in the selected folder or its subfolders.");
        return;
    }

    var originalInteractionLevel = app.userInteractionLevel;
    app.userInteractionLevel = UserInteractionLevel.DONTDISPLAYALERTS; // Suppress Illustrator's own dialogs
    
    var errorFiles = []; // To keep track of files that failed

    // Process each AI file
    for (var i = 0; i < aiFiles.length; i++) {
        var currentFile = aiFiles[i];
        // Double-check for '._' files, though collectAIFilesRecursively should filter most
        if (currentFile.name.substring(0, 2) === "._") {
            $.writeln("Skipping metadata file: " + currentFile.name);
            continue; 
        }

        try {
            // Open the AI file
            var doc = app.open(currentFile);
            
            // Get the filename without extension
            var fileName = currentFile.name.replace(/\.[^\.]+$/, '');
            
            // Create PNG export options
            var exportOptions = new ExportOptionsPNG24();
            exportOptions.antiAliasing = true;
            exportOptions.transparency = true;
            exportOptions.artBoardClipping = true;
            
            // Set the output file path
            // Ensure the output folder exists (it should, as it was selected)
            var outputFile = new File(outputFolder.fsName + "/" + fileName + ".png");
            
            // Export as PNG
            doc.exportFile(outputFile, ExportType.PNG24, exportOptions);
            
            // Close the document without saving
            doc.close(SaveOptions.DONOTSAVECHANGES);
            
            // Progress feedback
            $.writeln("Converted: " + currentFile.name + " to " + fileName + ".png");
            
        } catch (error) {
            // Log error to console instead of alert
            $.writeln("ERROR processing " + currentFile.name + ": " + error.message);
            errorFiles.push(currentFile.name); // Log the name of the file that caused an error
            // Attempt to close the document if it was opened before the error occurred
            if (app.documents.length > 0 && app.activeDocument.fullName.fsName === currentFile.fsName) {
                try {
                    app.activeDocument.close(SaveOptions.DONOTSAVECHANGES);
                } catch (closeError) {
                    $.writeln("Additionally, error closing problematic file " + currentFile.name + ": " + closeError.message);
                }
            }
        }
    }
    
    app.userInteractionLevel = originalInteractionLevel; // Restore original interaction level

    var successCount = aiFiles.length - errorFiles.length;
    var summaryMessage = "Conversion complete!\nProcessed " + aiFiles.length + " files.\nSuccessfully converted: " + successCount;
    if (errorFiles.length > 0) {
        summaryMessage += "\nEncountered errors with " + errorFiles.length + " files:\n" + errorFiles.join("\n");
        summaryMessage += "\n(Check Illustrator's JavaScript console for detailed error messages)";
    }
    alert(summaryMessage);
}

// Alternative version that processes only the currently open document
function convertCurrentDocToPNG() {
    if (app.documents.length === 0) {
        alert("No document is currently open.");
        return;
    }
    
    var doc = app.activeDocument;
    
    // Get the output folder
    var outputFolder = Folder.selectDialog("Select output folder for PNG file:");
    if (!outputFolder) {
        alert("No output folder selected. Script cancelled.");
        return;
    }
    
    // Get the document name without extension
    var fileName = doc.name.replace(/\.[^\.]+$/, '');
    
    // Create PNG export options
    var exportOptions = new ExportOptionsPNG24();
    exportOptions.antiAliasing = true;
    exportOptions.transparency = true;
    exportOptions.artBoardClipping = true;
    
    // Set the output file path
    var outputFile = new File(outputFolder + "/" + fileName + ".png");
    
    try {
        // Export as PNG
        doc.exportFile(outputFile, ExportType.PNG24, exportOptions);
        alert("Successfully exported: " + fileName + ".png");
    } catch (error) {
        alert("Error exporting file: " + error.message);
    }
}

// Run the batch conversion
convertAItoPNG();

// Uncomment the line below instead if you want to convert only the current document
// convertCurrentDocToPNG();
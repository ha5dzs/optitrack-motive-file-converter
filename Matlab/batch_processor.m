% Batch processor script. It calls the external converter executable.
clear;
clc;

%% Edit these main variables.
executable_location = 'converter\converter.exe';

take_file_directory = 'X:\Zoltan\test_data\motive_raw_files';
csv_file_directory = 'X:\Zoltan\test_data\converted_marker_paths';

%% Check for files

take_directory_list = dir(fullfile(take_file_directory,'*.tak')); % List all take files

export_directory_list = dir(fullfile(csv_file_directory,'*.csv')); % List all csv files

if(~length(take_directory_list))
    error('take_file_directory points where there are no *.tak files.')
end

if(length(export_directory_list))
    warning('export_directory_list points to a directory that is not empty.')
    fprintf('This script will overwrite these files. Press ENTER in the command window to proceed or Ctrl+C to stop.\n')
    pause;
    fprintf('OK, we carry on. You have been warned.\n')
end

%% Parallel pool.

if(~canUseParallelPool)
    % Start the parallel pool if not already running.
    parpool local;
end

%% Create the strings to execute

% But first, we create the strings to execute
command_string_array = cell(length(take_directory_list), 1);
for i = 1:length(take_directory_list)
    % Assemble the strings
    command_string_array{i} = sprintf( ...
        '%s "%s\\%s" "%s\\%s.csv"', ...
        executable_location, ...
        take_file_directory, take_directory_list(i).name, ...
        csv_file_directory, take_directory_list(i).name);
end


%% Execute parallel loop
tic;
parfor i = 1:length(take_directory_list)
    % Do this in the system.
    % fprintf('%s: ', take_directory_list(i).name); % Show what we are working on
    if(system(command_string_array{i}))
        % We are abusing the system, something doesn't like being loaded
        % multiple times. When this happens, we get exceptions about the
        % file not being found. The file is there, we checked it eariler.
        fprintf('\n[WARNING]: You may need to re-do Trial %d.\t', i)
        fprintf('Just execute ''system(command_string_array{%d})''\n', i)
    end
        
end

fprintf('If you need to re-do something: ''system(command_string_array{trial_number})''\n');

fprintf('\nThis operation took %d minutes.\n', round(toc/60))
# Conveyor-Bucket-Sorter

There is the huge file, with size approximately from 1 to 100 gb. 
Structure of file is '[number] . [string]". Example:

	1001 . London is the capital
	854 . My name is Viktor
	9999 . I'm a boy

Strings should sort first; if they are equal, you should sort by number.

#### Example unsorted file:

	981 . An old man
	721 . An old woman
	17 . I love rock and roll
	777 . Fill the power of the force.
	10001 . Ann is a girl
	99 . Abraham was a president
	666 . Fill the power of the force.

#### Result in sorted file:

	99 . Abraham was a president
	981 . An old man
	721 . An old woman
	10001 . Ann is a girl
	666 . Fill the power of the force.
	777 . Fill the power of the force.
	17 . I love rock and roll

## In result you should have:
  1) Application that randomly generates unsorted file.
  2) Application that would read unsorted file and sort it as described.
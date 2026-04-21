#!/bin/bash
# Helper script to run dotnet watch for the Uniflow project
# This works around the issue where dotnet watch gets confused with multiple project files

# Temporarily hide the solution file
# Temporarily hide the solution file
mv Uniflow.sln Uniflow.sln.temp 2>/dev/null

# Run dotnet watch
dotnet watch run

# Restore the solution file
mv Uniflow.sln.temp Uniflow.sln 2>/dev/null

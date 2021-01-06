#!/bin/bash

echo "Altcover:$1"
echo "Threshold:$2"
echo "Assembly exclude filter:$3"
echo "Solution:$4"
echo "Build configuration:$5"

# /p:AltCoverFileFilter=\.*JunitTestLogger\.* \

CMD="dotnet test \
--no-build \
/p:AltCover=$1 \
/p:AltCoverForce=true \
/p:AltCoverThreshold=$2 \
/p:AltCoverFileFilter=\.*JunitTestLogger\.* \
/p:AltCoverAssemblyExcludeFilter=\"$3\" \
/p:AltCoverLocalSource=true \
$4 \
--configuration $5
"

echo "CMD is: $CMD"

eval $CMD

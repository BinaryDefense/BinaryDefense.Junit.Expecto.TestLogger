#!/bin/bash

dotnet test --logger:"junit;$1"
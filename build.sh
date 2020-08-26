#! /bin/sh

ROSYLN="/c/Program Files (x86)/Microsoft Visual Studio/2019/BuildTools/MSBuild/Current/Bin/Roslyn/"
DOTNET="/c/Windows/Microsoft.NET/Framework64/v4.0.30319/"

OBJDIR=.obj
CILTOOL=Bluebonnet.exe

CFLAGS="-optimize+ -shared -platform:x64 -langversion:latest"
DEBUGFLAGS="-define:DEBUGDIAG -debug+"

######################################################################################

function BuildProgram()
{

if [ ! -d "$OBJDIR" ]; then mkdir $OBJDIR; fi

LIBS_SYSTEM=(
    # spaces and parentheses are okay in a double-quoted path. backslash not needed.
    "$DOTNET/mscorlib.dll"
    "$DOTNET/System.dll"
    "$DOTNET/System.IO.Compression.dll"
)
LIBS_BUNDLED=(
    # spaces and parentheses are okay in a double-quoted path. backslash not needed.
    "packages/Mono.Cecil.0.11.2/lib/net40/Mono.Cecil.dll"
    "packages/Mono.Cecil.0.11.2/lib/net40/Mono.Cecil.Pdb.dll"
    "packages/Mono.Cecil.0.11.2/lib/net40/Mono.Cecil.Mdb.dll"
)

LIBS_ARG=()
for lib in "${LIBS_SYSTEM[@]}" "${LIBS_BUNDLED[@]}"; do
    LIBS_ARG+=("-r:${lib}")
done

if [ "$DEBUG" == "1" ]; then
    DEBUG=$DEBUGFLAGS
fi

"$ROSYLN/csc.exe" $DEBUG $CFLAGS -nostdlib+ "-lib:$DOTNET" "${LIBS_ARG[@]}" JavaBinary/src/*.cs CilToJava/src/*.cs Main/src/*.cs -out:$OBJDIR/$CILTOOL
if [ $? != 0 ]; then
    exit 1
fi

for lib in "${LIBS_BUNDLED[@]}"; do
    if [ "$lib" -nt "$OBJDIR/`basename "$lib"`" ]; then
        cp -v "$lib" "$OBJDIR/`basename "$lib"`"
    fi
done
}

######################################################################################

function BuildLibrary()
{

JAVALIB=Javalib.dll

#
# java runtime classes rt.jar --> .Net assembly javalib.dll
#

if [ ! -f "$OBJDIR/$JAVALIB" ]; then

    if [ ! -f "$OBJDIR/$CILTOOL" ]; then
        echo error: please build program as "$OBJDIR/$CILTOOL"
        exit 1
    fi
    "$OBJDIR/$CILTOOL" "$JAVA_HOME/jre/lib/rt.jar" "$OBJDIR/$JAVALIB"
    if [ $? != 0 ]; then
        if [ -z "$JAVA_HOME" -o ! -f "$JAVA_HOME/jre/lib/rt.jar" ]; then
            echo error: please set environment variable JAVA_HOME
        fi
        exit 1
    fi

fi

#
# compile our .Net runtime library --> Baselib.jar
#

if [ "$DEBUG" != "0" ]; then
    DEBUG=$DEBUGFLAGS
else
    DEBUG=""
fi

shopt -s globstar
rm -rf "$OBJDIR/Baselib.pdb"
"$ROSYLN/csc.exe" $DEBUG $CFLAGS -r:"$OBJDIR/$JAVALIB" Baselib/src/**/*.cs -t:library -out:"$OBJDIR/Baselib.dll"
if [ $? != 0 ]; then
    exit 1
fi
rm -f "$OBJDIR/Baselib.jar"
"$OBJDIR/$CILTOOL" "$OBJDIR/Baselib.dll" "$OBJDIR/Baselib.jar"
if [ $? != 0 ]; then
    exit 1
fi

#
# some .Net runtime types --> Baselib.jar
#

for FilterFile in Baselib/*.filter; do

    FILTER_ARG=()
    while IFS=$' \t\n\r' read -r type; do
        if [ -n "${type}" ]; then
            FILTER_ARG+=(":${type}")
        fi
    done < $FilterFile

    FILTER_DLL=`basename $FilterFile .filter`.dll
    "$OBJDIR/$CILTOOL" "**/$FILTER_DLL" "$OBJDIR/Baselib.jar" "${FILTER_ARG[@]}"
    if [ $? != 0 ]; then
        exit 1
    fi

done

}

######################################################################################

function BuildTest()
{
    if [ "$DEBUG" != "0" ]; then
        DEBUG=$DEBUGFLAGS
    else
        DEBUG=""
    fi

    mkdir -p $OBJDIR/test
    rm -rf "$OBJDIR/test/$1.pdb"
    "$ROSYLN/csc.exe" $DEBUG $CFLAGS -unsafe -define:STANDALONE \
        -r:mscorlib.dll -r:System.Runtime.dll "-r:$OBJDIR/Javalib.dll" \
        -r:packages/MSTest.TestFramework.2.1.0/lib/net45/Microsoft.VisualStudio.TestPlatform.TestFramework.dll \
        Tests/src/$1.cs Tests/src/BaseTest.cs -out:$OBJDIR/test/$1.exe
    if [ $? -ne 0 ]; then
        echo Test failed to compile
        exit 1;
    fi
    echo ========== Running Original Test ==========
    if ! ( "$OBJDIR/test/$1.exe" ); then
        echo Test failed even before conversion
        exit 1
    fi
    echo ========== Converting exe to jar ==========
    rm -f "$OBJDIR/test/$1.jar"
    if ! ( "$OBJDIR/$CILTOOL" "$OBJDIR/test/$1.exe" "$OBJDIR/test/$1.jar" ); then
        echo Test failed conversion
        exit 1
    fi
    echo ========== Running Converted Test ==========
    "$JAVA_HOME/bin/java.exe" -Xdiag -Xverify:all -cp "$OBJDIR/test/$1.jar:$OBJDIR/baselib.jar" "tests.BaseTest" "$1"
}

######################################################################################

case "$1" in
    Test*)
        BuildTest $1
        ;;
    release)
        cmd "/c build.bat"
        ;;
    *lib)
        BuildLibrary
        ;;
    clean*)
        echo deleting .obj and .vs
        rm -rf .obj .vs
        ;;
    *)
        BuildProgram
        ;;
esac

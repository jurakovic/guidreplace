#!/bin/bash

# Default values
COMMAND="publish"
BRANCH=""
COMMIT=""
MESSAGE=""
VERSION="auto"
BUMP_TYPE="patch"
PREVIEW="auto"
ARCH=""
UNATTENDED="false"

# Allowed values
_command_values=("publish" "package" "release")
_bump_type_values=("major" "minor" "patch")
_preview_values=("auto" "true" "false")
_arch_values=("win-x64" "linux-x64" "osx-x64")
_unattended_values=("true" "false")

# Colors
Color_Off='\033[0m'
Color_Green='\033[0;32m'
Color_Yellow='\033[0;33m'

function main() {
  read_args "$@"

  case "$COMMAND" in
    "publish") publish ;;
    "package") package ;;
    "release") release ;;
  esac
}

function publish() {
  rm -rf src/bin/ src/obj/
  dotnet publish src -c Release -r $ARCH --self-contained -p:AssemblyVersion="$(echo $VERSION | sed 's/-preview//')" -p:Version="$VERSION" -o ./publish/$ARCH

  if [[ ! $? -eq 0 ]]; then exit 1; fi # exit if build failed or canceled

  echo
  echo -e "${Color_Green}Publish OK${Color_Off}"
}

function package() {
  local publish_path="publish/$ARCH"
  local assembly_name="guidrep"
  local assembly_ext=""
  local shacmd=""

  case "$ARCH" in
    win*) assembly_ext=".exe" ;;
  esac

  case "$ARCH" in
    lin*) shacmd="sha256sum" ;;
    win*) shacmd="sha256sum$assembly_ext" ;;
    mac*) shacmd="shasum -a 256" ;;
  esac

  local release_path="release/$ARCH"
  local archive_name="${assembly_name}_${VERSION}_${ARCH}.zip"
  local archive_path="$release_path/../$archive_name"
  local assembly_full="${assembly_name}${assembly_ext}"

  mkdir -p $release_path
  cp "$publish_path/$assembly_full" "$release_path"

  cd $release_path && zip -r -9 "../$archive_name" "$assembly_full" && cd - > /dev/null

  local sha256=$($shacmd "$archive_path" | cut -d " " -f 1)
  echo "$sha256  $archive_name" >> $release_path/../checksums.txt

  echo -e "${Color_Green}Package OK${Color_Off}"
}

function release() {
  if [ ! $UNATTENDED = "true" ]
  then
    read -p "Do you want to continue to git commit, tag, push...? (y/n) " yn
    if [ ! $yn = "y" ]; then exit; fi
  fi

  git checkout release >/dev/null 2>&1 && git pull || git checkout -b release origin/release --force >/dev/null 2>&1 || git checkout --orphan release && git reset --hard

  local url="$(git remote get-url origin | sed -E 's/\.git$//')/releases/tag/v$VERSION"

  echo "VERSION:   $VERSION"    > version
  echo "BUMP_TYPE: $BUMP_TYPE" >> version
  echo "PREVIEW:   $PREVIEW"   >> version
  echo "BRANCH:    $BRANCH"    >> version
  echo "COMMIT:    $COMMIT"    >> version
  echo "URL:       $url"       >> version

  git add version
  git commit -m "v$VERSION ($BRANCH)"
  git push --set-upstream origin release

  git checkout "$BRANCH"
  git tag "v$VERSION"
  git push origin "v$VERSION"

  echo -e "${Color_Yellow}Version $VERSION released${Color_Off}"
  echo -e "${Color_Green}All done${Color_Off}"
}

function read_args() {
  while [ "${1:-}" != "" ]; do
    case "$1" in
      "-h" | "--help")
        help
        exit 0
        ;;
      "-v" | "--version")
        shift
        # todo: regex validate
        VERSION="$1"
        ;;
      "-b" | "--bump_type")
        shift
        if is_in_array "$1" "${_bump_type_values[@]}"; then
            BUMP_TYPE="$1"
        else
            invalid_argument "$1" "${_bump_type_values[*]}"
            exit 1
        fi
        ;;
      "-p" | "--preview")
        shift
        if is_in_array "$1" "${_preview_values[@]}"; then
            PREVIEW="$1"
        else
            invalid_argument "$1" "${_preview_values[*]}"
            exit 1
        fi
        ;;
      "-a" | "--arch")
        shift
        if is_in_array "$1" "${_arch_values[@]}"; then
            ARCH="$1"
        else
            invalid_argument "$1" "${_arch_values[*]}"
            exit 1
        fi
        ;;
      "-u" | "--unattended")
        shift
        if is_in_array "$1" "${_unattended_values[@]}"; then
            UNATTENDED="$1"
        else
            invalid_argument "$1" "${_unattended_values[*]}"
            exit 1
        fi
        ;;
      *)
        if is_in_array "$1" "${_command_values[@]}"; then
            COMMAND="$1"
        else
            invalid_argument "$1" "${_command_values[*]}"
            exit 1
        fi
        ;;
    esac
    shift
  done

  BRANCH=$(git branch --show-current)
  COMMIT=$(git log -n 1 --format="%H")
  MESSAGE=$(git log -n 1 --format="%B")

  if [ "$PREVIEW" = "auto" ]; then
    if [ "$BRANCH" != "master" ]; then
      PREVIEW="true"
    else
      PREVIEW="false"
    fi
  fi

  if [ "$VERSION" = "auto" ]; then
    local previous=$(git show origin/release:version >/dev/null 2>&1 || echo '0.0.0' | head -n 1 | sed 's/VERSION:   //')
    echo "PREV_VER:   $previous"
    VERSION=$(bump_version "$previous" "$BUMP_TYPE" "$PREVIEW")
  else
    BUMP_TYPE=""
    PREVIEW=""
  fi

  echo "COMMAND:   $COMMAND"
  echo "VERSION:   $VERSION"
  echo "BUMP_TYPE: $BUMP_TYPE"
  echo "PREVIEW:   $PREVIEW"
  echo "ARCH:      $ARCH"
  echo "BRANCH:    $BRANCH"
  echo "COMMIT:    $COMMIT"
  echo "MESSAGE:   $MESSAGE"

  if [ $UNATTENDED = "true" ]
  then
    for i in {3..0..1}; do echo -en "\rContinue in $i" && if [ "$i" -gt "0" ]; then sleep 1; fi; done
    echo -en "\r             "
  else
    read -p "Press any key to continue..." -n1 -s; echo
  fi
  echo
}

function help() {
  echo "Usage:"
  echo "  ./publish.sh <command> [options]"
  echo ""
  echo "Commands:"
  echo "  publish                        Step 1: run dotnet publish"
  echo "  package                        Step 2: archive executable to zip or tarball"
  echo "  release                        Step 3: execute git commit, tag, push..."
  echo ""
  echo "Options:                         Allowed values:"
  echo "  -v, --version <VERSION>        auto*, x.y.z"
  echo "  -b, --bump_type <BUMP_TYPE>    major, minor, patch*"
  echo "  -p, --preview <PREVIEW>        auto*, true, false"
  echo "  -a, --arch <ARCH>              win-x64, linux-x64, osx-x64"
  echo "  -u, --unattended <UNATTENDED>  true, false*"
  echo "  -h, --help"
  echo "                                 * Default value"
  echo ""
  echo "Examples:"
  echo "  ./publish.sh publish"
  echo "  ./publish.sh publish -v 1.2.3"
  echo "  ./publish.sh publish -b major -p true"
}

function is_in_array() {
  local value="$1"
  shift
  for element in "$@"; do
    if [[ "$element" == "$value" ]]; then
      return 0
    fi
  done
  return 1
}

function invalid_argument() {
  echo "Error. Invalid argument value: $1"
  echo "Allowed values: ${*:2}"
  echo
  help
}

function bump_version() {
  local current_version="$1"
  local bump_type="$2"
  local preview="$3"

  IFS='-' read -ra chunks <<< $(echo $current_version | sed 's/-preview./-/')
  local current_main_version="${chunks[0]}"
  local current_prev_version="${chunks[1]:-0}"
  local next_version=""

  if [[ "$preview" == "false" ]]; then
    if [[ "$current_prev_version" -eq 0 ]]; then
      # Regular bump
      next_version=$(bump_version_main "$current_main_version" "$bump_type")
    else
      # Unpreview
      next_version="$current_main_version"
    fi
  else
    if [[ "$current_prev_version" -eq 0 ]]; then
      # First preview
      next_version=$(bump_version_main "$current_main_version" "$bump_type")
      next_version+="-preview.1"
    else
      # Bumping existing preview
      next_version="$current_main_version"
      next_version+="-preview.$((current_prev_version + 1))"
    fi
  fi

  echo "$next_version"
}

function bump_version_main() {
  local current_version="$1"
  local bump_type="$2"

  # Split the version into major, minor, and patch
  IFS='.' read -ra chunks <<< "$current_version"
  local major="${chunks[0]}"
  local minor="${chunks[1]:-0}"
  local patch="${chunks[2]:-0}"

  # Increment version based on bump type
  case "$bump_type" in
    major)
      ((major++))
      minor=0
      patch=0
      ;;
    minor)
      ((minor++))
      patch=0
      ;;
    patch)
      ((patch++))
      ;;
    *)
      echo "Invalid bump type: $bump_type"
      return 1
      ;;
  esac

  echo "${major}.${minor}.${patch}"
}

main "$@"

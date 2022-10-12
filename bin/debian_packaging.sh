#!/usr/bin/env bash
#---------------------------------------------------------------------------------------------
# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License. See License.txt in the project root for license information.
#---------------------------------------------------------------------------------------------

# Update and install required packages
apt-get update
apt-get install -y devscripts debhelper build-essential

wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

apt-get update
apt-get install -y dotnet-sdk-6.0
apt-get install -y dotnet-runtime-6.0

# Define variables
source_package_name="azureauth"
binary_package_name="azureauth"
upstream_version=0.5.1
debian_revision=1
underscore="_"
dash="-"
ROOT_DIR="."
DEST_FOLDER="azureauthdeb"
tar_file_extension=".orig.tar.gz"
debian_dir="debian"
TAB=$'\t'

# move to the root directory
cd $ROOT_DIR

# make a destination directory
mkdir -p $DEST_FOLDER
cd $DEST_FOLDER

# 1. Fetch the source code and pack it into a tarball
git_folder_name="$source_package_name$dash$upstream_version"
git clone https://github.com/AzureAD/microsoft-authentication-cli.git $git_folder_name
tar_file_name="$source_package_name$underscore$upstream_version$tar_file_extension"
tar -czvf $tar_file_name $git_folder_name

# Copy nuget.config file (temporary change)
cp ../nuget.config $git_folder_name/

# 2. Add Debian Packaging files
cd $git_folder_name
mkdir -p $debian_dir/source

# Changelog file
# https://stackoverflow.com/a/4879146
cat > $debian_dir/changelog << EOM
$source_package_name ($upstream_version-$debian_revision) UNRELEASED; urgency=low

  * Debian package release.

 -- ES365 Security Experience Team <es365se@microsoft.com>  $(date -R)

EOM

# Compat file
echo '10' > $debian_dir/compat

# Source format file
echo '3.0 (quilt)' > $debian_dir/source/format

# Control file
cat > $debian_dir/control << EOM
Source: $source_package_name
Maintainer: ES365 Security Experience Team <es365se@microsoft.com>
Section: misc
Priority: optional
Standards-Version: 3.9.2
Build-Depends: debhelper (>= 9)

Package: $binary_package_name
Architecture: all
Depends: \${shlibs:Depends}, \${misc:Depends}
Description: AzureAuth
    A CLI designed to authenticate and return an access token for public client AAD applications. 
    This acts like a credential provider for Azure Devops and any other public client app.

EOM

# Copyright file
echo '' > $debian_dir/copyright

# rules file
cat > $debian_dir/rules << EOM
#!/usr/bin/make -f

#export DH_VERBOSE=1
#export DH_OPTIONS=-v

%:
${TAB}dh \$@

override_dh_install:
${TAB}mkdir -p /usr/bin/azureauth
${TAB}dotnet clean
${TAB}dotnet restore --configfile "../../nuget.config"
${TAB}dotnet build
${TAB}dotnet publish src/AzureAuth/AzureAuth.csproj -p:Version=0.5.1 --configuration release --self-contained true --runtime linux-x64 --output dist/linux-x64
${TAB}cp -r dist/linux-x64 /usr/bin/azureauth

EOM

cat $debian_dir/rules

# debian directory should be executable
chmod 0755 $debian_dir

# 4.Build the package
debuild -us -uc
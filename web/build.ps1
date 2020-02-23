$version = ((Invoke-Expression "git describe --abbrev=0 --tags").Substring(1))
Invoke-Expression "docker build -t tobiasfenster/azdevops-wi-reader:$($version)-1809 -f Dockerfile.1809 ."
Invoke-Expression "docker images"
#Write-Host "docker login -u `"$($env:docker_user)`" -p `"$($env:docker_pwd)`" docker.io"
Write-Host "docker push `"tobiasfenster/azdevops-wi-reader:$($version)-1809`""
#echo "$($env:docker_pwd)" | docker login -u "$($env:docker_user)" --password-stdin
#Invoke-Expression "docker login -u `"$($env:docker_user)`" -p `"$($env:docker_pwd)`" docker.io"
Invoke-Expression "docker push `"tobiasfenster/azdevops-wi-reader:$($version)-1809`""
Invoke-Expression "docker logout"
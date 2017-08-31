P:
\tools\rsync\rsync -e \tools\rsync\ssh --chmod=D755,F644 --delete -av ./_site/ iniab@fsharpforfunandprofit.com:~/fsharpforfunandprofit.com/
REM --chmod=Du=rwx,Dgo=rx,Fu=rw,Fgo=r
pause
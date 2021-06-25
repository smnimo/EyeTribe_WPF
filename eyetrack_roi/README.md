# Eye tracking sample with sequence of RoI

## Display Image + capture and display gaze points

- output: gaze points (x, y) + RoI ID
- Input: 
    - image (Graphics/img.png)
    - RoI (roi.csv)
      - roi.csv:
        - 1st row: imagesize(x), imagesize(y), 0, 0, 0
        - 2nd row~: RoI ID, Left, Top, Width, Height

## output: filename--> yyMMddhhssmm.csv

- 1st row: image Region: x of Image in Displayed content, y of Image in Displayed content
- 2nd row~ (# of RoI): x of RoI in Displayed image, y of RoI in Displayed image
- after~: x of gaze points, y of gaze points, RoI ID

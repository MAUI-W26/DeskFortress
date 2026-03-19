# DeskFortress Dev Steps

## 1- Preparing assets:

### Images gathering + sanitization

- normalize format
- clear backgrounds
- reframe

### Images collision architecture (preparation + data gathering)

- design & define data storage system and shape

- define every collision zones on each relevant image asset

  <img src="C:\Users\a3emo\AppData\Roaming\Typora\typora-user-images\image-20260318114712422.png" alt="image-20260318114712422" style="zoom: 33%;" /><img src="C:\Users\a3emo\AppData\Roaming\Typora\typora-user-images\image-20260318115212419.png" alt="image-20260318115212419" style="zoom: 50%;" />

- design processing system & objectives (eg. overlap priority / pixel-base -> %-base(ratio-base) measurements / runtime vs compile-time calculations )

- data sanitizing design (separated processing app / preprocessing / storage vs in-memory) -> choice costs (processing / time-consuming)

### Audio assets

- Define needs
- Sfx + background music gathering
- Study framework / platform behavior
- Design audio management module
- Plan implementation
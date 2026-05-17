# *AssetBankPlugin*
A plugin for Frosty Toolsuite that adds AssetBank reading and exporting support for all major Frostbite titles.
# Contributing
Feel free to create an issue or a pull request if you think you have ideas or code to improve this plugin.

# Disclaimer
This plugin is actively being developed without thorough testing! I can't guarantee that current code will work or even compile.

# This is a fork attempting to add support for the vbr codec
The code is an absolute mess, here are some rambling about decoding

- each asset has vector, quat and float channels, whith const equivalents as well(floats will be ignored because as far as I can tell theyre always zero)
- quats have 4 values (include rotation) vectors have 3. whenever we read a channel, we need to read 4 values for each quat and 3 for each vector so we multiply their count to get the totals
- const channels are well, constant and read from the constant palette using some information in the header
- the assets can be parsed by the current plugin, however it also contains an additional "header" inside its data field (size of the data field -size of all frameblocks as defined in the header)
- the data header contains as follows
  - indexes for the constant palette for each const channel(constQuats*4+constVec*3)
  - a const channel map, size defined in the header, this is a list of values adding up to the constantpalette size +an unknown variable, appears to be some extra constant channels or empty channels which arent included     except in the dof mapping asset ( not clear where these go in the order but thats a later problem)
  - will go into more detail how this is used later(cant understand my deranged code)
  -  there is also the vector offsets,these are actually at the end of the header but theyre alot simpler,seems to be a count followed by a set of 3 bytes for each count, probably vector to add to, number of vectors to add to?, then the actual value, (only a very small number of these)
  - next is the important one, the numbit values for the variable channels
  - there are channel count*4 bytes for storing these values
  - stored in nibbles so channel count *8 values (very similar to dct)
  - likely these hold 8 values for each channel for each frameblock(block of 8 frame packed together)
  - need to do more testing to establish order
  - its most likely that in order to achieve the variable bit rate (name of the codec+ frameblocks are different sizes), they use some bitstream decoding to decide whether or not to read some of these bits ( if they dont change they dont read the value and then the frameblock can be smaller)
  - this is opposed to dct which just read all the values all the time
  - not totally sure how this works
  - total number of numbits in an example asset is 8846, with frameblock sizes of 11429, 11520,8952, etc (chan count = 
  - this may just be a bad example case where it dosent actually save any bits(makes it worse?), presumbably extra control bits are added which take up space
  - most likely way control bits work is just a 1 or 0 to read or not read

After that the data should hopefully be decompressed the same as dct (fingers crossed)
Then the dof channel mapping should be the same as for dct with the addition of these phantom channels as mentioned above
- dof channel mapping gives us a list of channels on the model( named after bones) in order which matches up with our channels if theyre in the correct order
- the const chan map basically tells us how many const channels to read, then how many spaces to leave for variable channels
- presumbably the variable channels just fill in the order theyre decompressed from the frameblock (always quats ->vectors)
- the index of a channel should relate to a value in the const palette, may need to add an offset equivalent to these "phantom" channels for this to line up
- 


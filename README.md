#ScryDraft

This is a simple windowed .xaml program designed for drafting in the Commander format of MTG.
It displays MTG cards gathered through the Scryfall API using URI requests, in correspondence with Scryfall's usage guidelines.

Draft is an incredibly fun format but came with some unique game design challenges when adapting to the massive card pool
and more value-based strategies of Commander. As such, there is a token system that guarantees the next draft pull to be
ramp, draw engines, removal or other core functionalites of a commander deck. Upon picking the first card as the commander,
all cards are filtered by color from that point on, so that all decks are playable.

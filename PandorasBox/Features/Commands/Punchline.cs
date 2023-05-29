using ECommons.Automation;
using ECommons.DalamudServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PandorasBox.Features.Commands
{
    public unsafe class Punchline : CommandFeature
    {
        public override string Name => "Punchline Auto Completer";
        public override string Command { get; set; } = "/pan-punchline";
        public override string[] Alias => new string[] { "/pan-pl" };

        public List<string> supposedlyJokes => new() {
            "...and that's how you milk a cow!",
            "...because seven eight (ate) nine!",
            "...because they can't find the door!",
            "...and that's why the chicken crossed the road!",
            "...it's a canary!",
            "...because it was too far to walk!",
            "...he wanted to see time fly!",
            "...they can't find the organ-izer!",
            "...because he wanted to see the butterfly!",
            "...because he had no body to go with him!",
            "...and that's why the circus always has a big top!",
            "...because it was two-tired!",
            "...because the steaks were too high!",
            "...and that's why the pencil sharpener was a great inventor!",
            "...because they can't stop bringing up the past!",
            "...because he wanted to catch up on the latest news!",
            "...because they had no guts!",
            "...because they had no body to go with!",
            "...because it couldn't find its keys!",
            "...because it didn't want to get caught up in the draft!",
            "...because it saw the salad dressing!",
            "...because it had a lot of drive!",
            "...and that's why the skeleton went to the party alone!",
            "...because he took a wrong turnip!",
            "...because he didn't carrot all!",
            "...because he was outstanding in his field!",
            "...because they always take things literally!",
            "...because it was soda-pressing!",
            "...because he wanted to catch the train of thought!",
            "...because they had too many laps to finish!",
            "...because they can't find the remote control!",
            "...because they had no common cents!",
            "...because she couldn't find a date!",
            "...because they wanted to make up their minds!",
            "...because they had too many problems to solve!",
            "...because they had no porpoise!",
            "...because he had an axe to grind!",
            "...because they couldn't resist the pun-ch line!",
            "...because they wanted to find out what all the buzz was about!",
            "...because it was a pressing issue!",
            "...because they were trying to keep up with the beet!",
            "...because they had a lot of baggage!",
            "...because he wanted to win by a hare!",
            "...because they wanted to win by a nose!",
            "...because they couldn't resist the incredible aroma!",
            "...because he couldn't keep his eyes peeled!",
            "...because they couldn't find any open mics!",
            "...because it couldn't handle the pressure!",
            "...because it was a slam dunk!",
            "...because they couldn't find the light switch!",
            "...because it was a light-hearted conversation!",
            "...because they wanted to be outstanding in their field!",
            "...because they wanted to find a matching pair!",
            "...because it was a bright idea!",
            "...because they were outstanding performers!",
            "...because they couldn't find the right keys!",
            "...because they wanted to stay sharp!",
            "...because it was a smashing success!",
            "...because they couldn't find the punchline!",
            "...because it was a piece of cake!",
            "...because they couldn't find the right formula!",
            "...because they couldn't find the magic wand!",
            "...because they wanted to take a bow!",
            "...because they wanted to be a cut above the rest!",
            "...because they wanted to put their best foot forward!",
            "...because they wanted to keep their cards close to their chest!",
            "...because it was an uplifting experience!",
            "...because they wanted to seize the day!",
            "...because they wanted to be a barrel of laughs!",
            "...because they wanted to be the cream of the crop!",
            "...because it was a slice of heaven!",
            "...because they wanted to be a smooth operator!",
            "...because they couldn't resist the temptation!",
            "...because they couldn't find the missing piece!",
            "...because they wanted to spice things up!",
            "...because they couldn't find the right rhythm!",
            "...because they couldn't find the right note!",
            "...because they wanted to seize the opportunity!",
            "...because they wanted to make a grand entrance!",
            "...because they wanted to be the talk of the town!",
            "...because they wanted to make a splash!",
            "...because they wanted to be a stroke of genius!",
            "...because they wanted to find the silver lining!",
            "...because they wanted to be a diamond in the rough!",
            "...because they wanted to be a breath of fresh air!",
            "...because they wanted to crack the code!",
            "...because they wanted to be a master of disguise!"
        };
        public override string Description => "don't ask why this exists";
        protected override void OnCommand(List<string> args)
        {
            Random random = new Random();
            Svc.Chat.Print($"{supposedlyJokes[random.Next(supposedlyJokes.Count)]}");
        }
    }
}

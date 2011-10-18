using System;
using System.Collections.Generic;
using System.Text;
using BlueBlocksLib.BaseClasses;
using BlueBlocksLib.SetUtils;

namespace BlueBlocksLib.StatisticalModels
{
    public class HiddenMarkovModel<TInput, TOutput>
    {
        double[/*pi*/] initial;
        double[/*x*/,/*x*/] transition;
        double[/*x*/,/*y*/] output;
        TInput[] inmap;
        TOutput[] outmap;
        Dictionary<TInput, int> inputSet = new Dictionary<TInput, int>();
        Dictionary<TOutput, int> outputSet = new Dictionary<TOutput, int>();


        public HiddenMarkovModel(
            double[/*pi*/] initial,
            double[/*x*/,/*x*/] transition,
            double[/*x*/,/*y*/] output,
            TInput[] inmap,
            TOutput[] outmap,
            Dictionary<TInput, int> inputSet,
            Dictionary<TOutput, int> outputSet)
        {
            Initialize(initial, transition, output, inmap, outmap, inputSet, outputSet);
        }

        private void Initialize(
            double[/*pi*/] initial, 
            double[/*x*/,/*x*/] transition, 
            double[/*x*/,/*y*/] output, 
            TInput[] inmap, 
            TOutput[] outmap, 
            Dictionary<TInput, int> inputSet,
            Dictionary<TOutput, int> outputSet)
        {
            this.initial = initial;
            this.transition = transition;
            this.output = output;
            this.inmap = inmap;
            this.outmap = outmap;
            this.inputSet = inputSet;
            this.outputSet = outputSet;
        }

        public delegate HiddenMarkovModel<TInput, TOutput> GenerationMethod(Expectation<int, int>[] training, TInput[] inmap, TOutput[] outmap, Dictionary<TInput, int> inputSet, Dictionary<TOutput, int> outputSet);

        public HiddenMarkovModel(Expectation<TInput, TOutput>[] initialData, GenerationMethod generationMethod)
        {

            Dictionary<TInput, int> inputSet = new Dictionary<TInput, int>();
            Dictionary<TOutput, int> outputSet = new Dictionary<TOutput, int>();

            // collect the types
            new List<Expectation<TInput, TOutput>>(initialData).ForEach(x =>
            {
                new List<Pair<TInput, TOutput>>(x.expectation).ForEach(y =>
                {
                    inputSet[y.a] = 0;
                    outputSet[y.b] = 0;
                });
            });

            // give them numbers
            TInput[] inmap = ArrayUtils.ToArray(inputSet.Keys);
            TOutput[] outmap = ArrayUtils.ToArray(outputSet.Keys);

            new List<int>(Number.Range(0, inmap.Length)).ForEach(x => { inputSet[inmap[x]] = x; });
            new List<int>(Number.Range(0, outmap.Length)).ForEach(x => { outputSet[outmap[x]] = x; });

            // make the new expectations
            Expectation<int, int>[] exp = ArrayUtils.ConvertAll(initialData, x => new Expectation<int, int>(
                ArrayUtils.ConvertAll(x.expectation, y => new Pair<int, int>()
                {
                    a = inputSet[y.a],
                    b = outputSet[y.b]
                })
            ));

            HiddenMarkovModel<TInput, TOutput> hmm = generationMethod(exp, inmap, outmap, inputSet, outputSet);
            this.Initialize(initial: hmm.initial,
                            transition: hmm.transition,
                            output: hmm.output,
                            inmap: hmm.inmap,
                            outmap: hmm.outmap,
                            inputSet: hmm.inputSet,
                            outputSet: hmm.outputSet);
        }

        public static HiddenMarkovModel<TInput, TOutput> AverageOccurance(Expectation<int, int>[] training, TInput[] inmap, TOutput[] outmap, Dictionary<TInput, int> inputSet, Dictionary<TOutput, int> outputSet)
        {

            int[] initialCount = new int[inmap.Length];
            int[,] transitionCount = new int[inmap.Length, inmap.Length];
            int[,] outputCount = new int[inmap.Length, outmap.Length];

            new List<Expectation<int, int>>(training).ForEach(x =>
            {
                // Count initials
                initialCount[x.expectation[0].a] += 1;

                // Count transitions
                new List<int>(Number.Range(0, x.expectation.Length - 1)).ForEach(index =>
                {
                    transitionCount[x.expectation[index].a, x.expectation[index + 1].a] += 1;
                });

                // Count outputs
                new List<int>(Number.Range(0, x.expectation.Length)).ForEach(index =>
                {
                    outputCount[x.expectation[index].a, x.expectation[index].b] += 1;
                });
            });

            int totalExamplesOfInitialStates = Number.Sum(initialCount);
            double[] initial = ArrayUtils.ConvertAll(initialCount, x => (double)x / (double)totalExamplesOfInitialStates);

            double[,] transition = new double[inmap.Length, inmap.Length];
            new List<int>(Number.Range(0, inmap.Length)).ForEach(state1 =>
            {
                int sumOfTransitions = Number.Sum((Array.ConvertAll(Number.Range(0, inmap.Length), state2 => transitionCount[state1, state2])));
                new List<int>(Number.Range(0, inmap.Length)).ForEach(state2 => transition[state1, state2] = (double)transitionCount[state1, state2] / (double)sumOfTransitions);
            });

            double[,] output = new double[inmap.Length, outmap.Length];
            new List<int>(Number.Range(0, inmap.Length)).ForEach(state =>
            {
                int sumOfOutputs = Number.Sum(Array.ConvertAll(Number.Range(0, outmap.Length), o => outputCount[state, o]));
                new List<int>(Number.Range(0, outmap.Length)).ForEach(o => output[state, o] = (double)outputCount[state, o] / (double)sumOfOutputs);
            });

            return new HiddenMarkovModel<TInput, TOutput>(
                initial,
                transition,
                output,
                inmap,
                outmap,
                inputSet,
                outputSet
            );
        }

        public double Viterbi(TOutput[] outputs, out TInput[] inputs)
        {
            int[] observations = ArrayUtils.ConvertAll(outputs, x => outputSet[x]);

            double[,] v = new double[outputs.Length, initial.Length];
            List<int>[] path = new List<int>[initial.Length];

            for (int y = 0; y < initial.Length; y++)
            {
                v[0, y] = initial[y] * output[y, observations[0]];
                path[y] = new List<int>();
                path[y].Add(y);
            }

            for (int t = 1; t < observations.Length; t++)
            {
                List<int>[] newpath = new List<int>[initial.Length];

                for (int y = 0; y < inmap.Length; y++)
                {

                    // find the highest probability path
                    double prob = 0;
                    int state = 0;
                    for (int y0 = 0; y0 < inmap.Length; y0++)
                    {
                        double thisprob = v[t - 1, y0] * transition[y0, y] * output[y, observations[t]];
                        int thisstate = y0;

                        if (prob < thisprob)
                        {
                            prob = thisprob;
                            state = thisstate;
                        }
                    }

                    v[t, y] = prob;
                    newpath[y] = new List<int>(path[state]);
                    newpath[y].Add(y);

                }

                path = newpath;
            }

            double finalprob = 0;
            int finalstate = 0;
            for (int y = 0; y < initial.Length; y++)
            {
                double thisprob = v[observations.Length - 1, y];
                int thisstate = y;
                if (finalprob < thisprob)
                {
                    finalprob = thisprob;
                    finalstate = thisstate;
                }
            }

            inputs = ArrayUtils.ConvertAll(path[finalstate].ToArray(), x => inmap[x]);
            return finalprob;
        }
    }
}

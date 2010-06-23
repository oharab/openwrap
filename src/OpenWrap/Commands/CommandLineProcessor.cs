﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenWrap.Commands
{
    public class CommandLineProcessor
    {
        readonly ICommandRepository _commands;

        public CommandLineProcessor(ICommandRepository commands)
        {
            _commands = commands;
        }

        public IEnumerable<ICommandResult> Execute(IEnumerable<string> strings)
        {
            if (strings == null || strings.Count() < 2)
            {
                yield return new NotEnoughParameters();
                yield break;
            }

            var matchingNouns = _commands.Nouns.Where(x => x.StartsWith(strings.ElementAt(0), StringComparison.OrdinalIgnoreCase)).ToList();
            if (matchingNouns.Count != 1)
            {
                yield return new NamesapceNotFound(matchingNouns);
                yield break;
            }
            var noun = matchingNouns[0];

            var matchingVerbs = _commands.Verbs.Where(x => x.StartsWith(strings.ElementAt(1), StringComparison.OrdinalIgnoreCase)).ToList();

            if (matchingVerbs.Count != 1)
            {
                yield return new UnknownCommand(strings.ElementAt(1), matchingVerbs);
                yield break;
            }

            var verb = matchingVerbs[0];

            var command = _commands.Get(noun, verb);

            var inputsFromCommandLine = ParseInputsFromCommandLine(strings.Skip(2)).ToLookup(x => x.Key, x => x.Value);

            var unnamedCommandInputsFromCommandLine = inputsFromCommandLine[null].ToList();

            var assignedNamedInputValues = (from namedValues in inputsFromCommandLine
                                            where namedValues.Key != null
                                            let value = namedValues.LastOrDefault()
                                            let commandInput = FindCommandInputDescriptor(command, namedValues.Key)
                                            let parsedValue = commandInput != null ? commandInput.ValidateValue(value) : null
                                            select new ParsedInput
                                            {
                                                InputName = namedValues.Key,
                                                RawValue = value,
                                                Input = commandInput,
                                                ParsedValue = parsedValue
                                            }).ToList();

            var namedInputsNameNotFound = assignedNamedInputValues.FirstOrDefault(x => x.Input == null);
            if (namedInputsNameNotFound != null)
            {
                yield return new UnknownCommandInput(namedInputsNameNotFound.InputName);
                yield break;
            }

            var namedInputsValueNotParsed = assignedNamedInputValues.FirstOrDefault(x => x.RawValue == null);
            //if (namedInputsValueNotParsed != null)
            //{
            //    yield return new InvalidCommandValue(namedInputsValueNotParsed.InputName, namedInputsValueNotParsed.RawValue);
            //    yield break;
            //}

            var inputNamesAlreadyFilled = assignedNamedInputValues.Select(x => x.InputName).ToList();
            // now got a clean set of input names that pass. Now on to the unnamed ones.

            var unfullfilledCommandInputs = (from input in command.Inputs
                                             where !inputNamesAlreadyFilled.Contains(input.Key, StringComparer.OrdinalIgnoreCase)
                                                   && input.Value.Position >= 0
                                             orderby input.Value.Position ascending
                                             select input.Value).ToList();

            if (unnamedCommandInputsFromCommandLine.Count > unfullfilledCommandInputs.Count)
            {
                yield return new InvalidCommandValue(unnamedCommandInputsFromCommandLine);
                yield break;
            }

            var assignedUnnamedInputValues = (from unnamedValue in unnamedCommandInputsFromCommandLine
                                              let input = unfullfilledCommandInputs[unnamedCommandInputsFromCommandLine.IndexOf(unnamedValue)]
                                              let commandValue = input.ValidateValue(unnamedValue)
                                              select new ParsedInput
                                              {
                                                  InputName = null,
                                                  RawValue = unnamedValue,
                                                  Input = input,
                                                  ParsedValue = commandValue
                                              }).ToList();


            var unnamedFailed = assignedUnnamedInputValues.Where(x => x.ParsedValue == null).ToList();
            if (unnamedFailed.Count > 0)
            {
                yield return new InvalidCommandValue(unnamedCommandInputsFromCommandLine);
                yield break;
            }

            var allAssignedInputs = assignedNamedInputValues.Concat(assignedUnnamedInputValues);

            allAssignedInputs = TryAssignSwitchParameters(allAssignedInputs, command.Inputs);

            var missingRequiredInputs = command.Inputs.Select(x => x.Value)
                                              .Where(x => !allAssignedInputs.Select(i => i.Input).Contains(x) && x.IsRequired)
                                              .ToList();
            if (missingRequiredInputs.Count > 0)
            {
                yield return new MissingCommandValue(missingRequiredInputs);
                yield break;
            }

            var missingInputValues = allAssignedInputs.Select(x => x.Input);
            // all clear, assign and run
            var commandInstance = command.Create();
            foreach (var namedInput in allAssignedInputs)
                namedInput.Input.SetValue(commandInstance, namedInput.ParsedValue);
            foreach (var nestedResult in commandInstance.Execute())
                yield return nestedResult;
        }

        IEnumerable<ParsedInput> TryAssignSwitchParameters(IEnumerable<ParsedInput> allAssignedInputs, IDictionary<string, ICommandInputDescriptor> inputs)
        {
            return allAssignedInputs;
        }

        ICommandInputDescriptor FindCommandInputDescriptor(ICommandDescriptor command, string name)
        {
            ICommandInputDescriptor descriptor;
            // Try to find by full name match first.
            if (command.Inputs.TryGetValue(name, out descriptor))
            {
                return descriptor;
            }
            var potentialInputs = from input in command.Inputs
                                  where name.MatchesHumps(input.Key)
                                  select input.Value;

            //var potentialInputs =
            //    from input in command.Inputs
            //    where input.Key.GetCamelCaseInitials().Equals(name, StringComparison.OrdinalIgnoreCase)
            //    select input.Value;

            // TODO: What happens if the name matches more than one input? Should this throw?
            // For now, just return the first input found.
            return potentialInputs.FirstOrDefault();
        }



        IEnumerable<KeyValuePair<string, string>> ParseInputsFromCommandLine(IEnumerable<string> strings)
        {
            string key = null;
            foreach (var component in strings)
            {
                if (component.StartsWith("-"))
                {
                    if (key != null)
                        yield return new KeyValuePair<string, string>(key, null);
                    key = component.Substring(1);
                    continue;
                }
                if (key != null)
                {
                    yield return new KeyValuePair<string, string>(key, component);
                    key = null;
                    continue;
                }
                yield return new KeyValuePair<string, string>(null, component);
            }
            if (key != null)
                yield return new KeyValuePair<string, string>(key, null);
        }

        class ParsedInput
        {
            public ICommandInputDescriptor Input;
            public string InputName;
            public object ParsedValue;
            public string RawValue;
        }
    }
}
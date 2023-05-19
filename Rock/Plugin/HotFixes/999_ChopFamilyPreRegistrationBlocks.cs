// <copyright>
// Copyright by the Spark Development Network
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rock.Plugin.HotFixes
{
    /// <summary>
    /// Plug-in migration
    /// </summary>
    /// <seealso cref="Rock.Plugin.Migration" />
    [MigrationNumber( 999, "1.15.0" )]
    public class ChopFamilyPreRegistrationBlocks : Migration
    {
        private static readonly string JobClassName = "PostV15DataMigrationsReplaceWebFormsBlocksWithObsidianBlocks";
        private static readonly string FullyQualifiedJobClassName = $"Rock.Jobs.{JobClassName}";

        /// <summary>
        /// Operations to be performed during the upgrade process.
        /// </summary>
        public override void Up()
        {
            ReplaceWebFormsBlocksWithObsidianBlocksUp();
        }

        /// <summary>
        /// Operations to be performed during the downgrade process.
        /// </summary>
        public override void Down()
        {
            // Down migrations are not yet supported in plug-in migrations.
        }

        /// <summary>
        /// JMH: Creates the run-once job that replaces WebForms blocks with Obsidian blocks.
        /// </summary>
        private void ReplaceWebFormsBlocksWithObsidianBlocksUp()
        {
            // Configure run-once job by modifying these variables.
            var commandTimeout = 14000;
            var blockTypeReplacements = new Dictionary<string, string>
            {
                // TODO JMH Chopping the Login Block (but should be pre-registration)
                //{ "463A454A-6370-4B4A-BCA1-415F2D9B0CB7", "" }
                // WebForms Login Block => Obsidian Login Block
                { "7B83D513-1178-429E-93FF-E76430E038E4", "5437C991-536D-4D9C-BE58-CBDB59D1BBB3" }
            };
            var shouldNotDeleteOldBlocks = false;
            // Delete WebForms Login Block
            var shouldDeleteOldBlockType = true;

            // Schedule the run-once job.
            Sql( $@"IF NOT EXISTS( SELECT [Id] FROM [ServiceJob] WHERE [Class] = '{FullyQualifiedJobClassName}' AND [Guid] = '{SystemGuid.ServiceJob.DATA_MIGRATIONS_150_REPLACE_WEB_FORMS_BLOCKS_WITH_OBSIDIAN_BLOCKS}' )
    BEGIN
        INSERT INTO [ServiceJob] (
            [IsSystem]
            ,[IsActive]
            ,[Name]
            ,[Description]
            ,[Class]
            ,[CronExpression]
            ,[NotificationStatus]
            ,[Guid] )
        VALUES ( 
            0
            ,1
            ,'Rock Update Helper v15.0 - Replace WebForms Blocks with Obsidian Blocks'
            ,'This job will replace WebForms blocks with their Obsidian blocks on all sites, pages, and layouts.'
            ,'{FullyQualifiedJobClassName}'
            ,'0 0 21 1/1 * ? *'
            ,1
            ,'{Rock.SystemGuid.ServiceJob.DATA_MIGRATIONS_150_REPLACE_WEB_FORMS_BLOCKS_WITH_OBSIDIAN_BLOCKS}'
            );
    END" );

            // Attribute: Rock.Jobs.PostV15DataMigrationsReplaceWebFormsBlocksWithObsidianBlocks: Command Timeout
            var commandTimeoutAttributeGuid = "F4C7151F-864A-4E36-8AF7-79D27DB41C07";
            RockMigrationHelper.AddOrUpdateEntityAttribute( "Rock.Model.ServiceJob", Rock.SystemGuid.FieldType.INTEGER, "Class", FullyQualifiedJobClassName, "Command Timeout", "Command Timeout", "Maximum amount of time (in seconds) to wait for each SQL command to complete. On a large database with lots of transactions, this could take several minutes or more.", 0, "14000", commandTimeoutAttributeGuid, "CommandTimeout" );
            RockMigrationHelper.AddServiceJobAttributeValue( Rock.SystemGuid.ServiceJob.DATA_MIGRATIONS_150_REPLACE_WEB_FORMS_BLOCKS_WITH_OBSIDIAN_BLOCKS, commandTimeoutAttributeGuid, commandTimeout.ToString() );

            // Attribute: Rock.Jobs.PostV15DataMigrationsReplaceWebFormsBlocksWithObsidianBlocks: Block Type Guid Replacement Pairs
            var blockTypeReplacementsAttributeGuid = "9431CD4D-A25A-4730-8724-5D107C6CDDA5";
            RockMigrationHelper.AddOrUpdateEntityAttribute( "Rock.Model.ServiceJob", Rock.SystemGuid.FieldType.KEY_VALUE_LIST, "Class", FullyQualifiedJobClassName, "Block Type Guid Replacement Pairs", "Block Type Guid Replacement Pairs", "The key-value pairs of replacement BlockType.Guid values, where the key is the existing BlockType.Guid and the value is the new BlockType.Guid. Blocks of BlockType.Guid == key will be replaced by blocks of BlockType.Guid == value in all sites, pages, and layouts.", 1, "", blockTypeReplacementsAttributeGuid, "BlockTypeGuidReplacementPairs" );
            RockMigrationHelper.AddServiceJobAttributeValue( Rock.SystemGuid.ServiceJob.DATA_MIGRATIONS_150_REPLACE_WEB_FORMS_BLOCKS_WITH_OBSIDIAN_BLOCKS, blockTypeReplacementsAttributeGuid, SerializeDictionary( blockTypeReplacements ) );

            // Attribute: Rock.Jobs.PostV15DataMigrationsReplaceWebFormsBlocksWithObsidianBlocks: Should Keep Old Blocks
            var shouldKeepOldBlocksAttributeGuid = "A1B097B3-310B-445E-ADED-80AB1EFFCEC6";
            RockMigrationHelper.AddOrUpdateEntityAttribute( "Rock.Model.ServiceJob", Rock.SystemGuid.FieldType.BOOLEAN, "Class", FullyQualifiedJobClassName, "Should Keep Old Blocks", "Should Keep Old Blocks", "Determines if old blocks should be kept instead of being deleted. By default, old blocks will be deleted.", 2, "False", shouldKeepOldBlocksAttributeGuid, "ShouldKeepOldBlocks" );
            RockMigrationHelper.AddServiceJobAttributeValue( Rock.SystemGuid.ServiceJob.DATA_MIGRATIONS_150_REPLACE_WEB_FORMS_BLOCKS_WITH_OBSIDIAN_BLOCKS, shouldKeepOldBlocksAttributeGuid, shouldNotDeleteOldBlocks.ToTrueFalse() );

            // Attribute: Rock.Jobs.PostV15DataMigrationsReplaceWebFormsBlocksWithObsidianBlocks: Should Delete Old Block Type
            var shouldDeleteOldBlockTypeAttributeGuid = "11CE14D8-C570-4E05-9802-6DCE6CBACD2A";
            RockMigrationHelper.AddOrUpdateEntityAttribute( "Rock.Model.ServiceJob", Rock.SystemGuid.FieldType.BOOLEAN, "Class", FullyQualifiedJobClassName, "Should Delete Old Block Type", "Should Delete Old Block Type", "Determines if the old block type should be deleted after the blocks are replaced. By default, the old block type will not be deleted.", 3, "False", shouldDeleteOldBlockTypeAttributeGuid, "ShouldDeleteOldBlockType" );
            RockMigrationHelper.AddServiceJobAttributeValue( Rock.SystemGuid.ServiceJob.DATA_MIGRATIONS_150_REPLACE_WEB_FORMS_BLOCKS_WITH_OBSIDIAN_BLOCKS, shouldDeleteOldBlockTypeAttributeGuid, shouldDeleteOldBlockType.ToTrueFalse() );
        }

        /// <summary>
        /// JMH: Removes the run-once job that replaces WebForms blocks with Obsidian blocks.
        /// </summary>
        private void ReplaceWebFormsBlocksWithObsidianBlocksDown()
        {
            Sql( $"DELETE FROM [ServiceJob] WHERE [Guid] = '{Rock.SystemGuid.ServiceJob.DATA_MIGRATIONS_150_REPLACE_WEB_FORMS_BLOCKS_WITH_OBSIDIAN_BLOCKS}'" );
            Sql( $"DELETE V FROM [AttributeValue] V INNER JOIN [Attribute] A ON A.[Id] = V.[AttributeId] WHERE A.[EntityTypeQualifierColumn] = 'Class' AND A.[EntityTypeQualifierValue] = '{FullyQualifiedJobClassName}" );
            Sql( $"DELETE FROM [Attribute] WHERE [EntityTypeQualifierColumn] = 'Class' AND [EntityTypeQualifierValue] = '{FullyQualifiedJobClassName}" );
        }

        private string SerializeDictionary( Dictionary<string, string> dictionary )
        {
            const string keyValueSeparator = "^";

            if ( dictionary?.Any() != true )
            {
                return string.Empty;
            }

            var sb = new StringBuilder();

            var first = dictionary.First();
            sb.Append( $"{first.Key}{keyValueSeparator}{first.Value}" );

            foreach ( var kvp in dictionary.Skip( 1 ) )
            {
                sb.Append( $"|{kvp.Key}{keyValueSeparator}{kvp.Value}" );
            }

            return sb.ToString();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using FluentSecurity.Policy;
using FluentSecurity.Specification.Helpers;
using FluentSecurity.Specification.TestData;
using NUnit.Framework;
using Rhino.Mocks;

namespace FluentSecurity.Specification
{
	[TestFixture]
	[Category("FluentSecuritySpec")]
	public class When_setting_the_configuration_for_fluent_security
	{
		[Test]
		public void Should_have_configuration()
		{
			// Arrange
			FluentSecurity.Reset();
			var configuration = MockRepository.GenerateMock<ISecurityConfiguration>();

			// Act
			FluentSecurity.SetConfiguration(configuration);

			// Assert
			Assert.That(FluentSecurity.CurrentConfiguration, Is.EqualTo(configuration));
		}

		[Test]
		public void Should_throw_ArgumentNullException_when_configuration_is_null()
		{
			// Arrange
			FluentSecurity.Reset();
			const ISecurityConfiguration nullConfiguration = null;

			// Act & Assert
			Assert.Throws<ArgumentNullException>(() => FluentSecurity.SetConfiguration(nullConfiguration));
		}
	}

	[TestFixture]
	[Category("FluentSecuritySpec")]
	public class When_calling_reset_on_fluent_security
	{
		[Test]
		public void Should_create_new_configuration_instance()
		{
			// Arrange
			var configuration = MockRepository.GenerateMock<ISecurityConfiguration>();
			FluentSecurity.SetConfiguration(configuration);

			// Act
			FluentSecurity.Reset();

			// Assert
			Assert.That(FluentSecurity.CurrentConfiguration, Is.Not.EqualTo(configuration));
		}
	}

	[TestFixture]
	[Category("FluentSecuritySpec")]
	public class When_calling_configure_on_fluent_security
	{
		private Action<ConfigurationExpression> _configurationExpression;
		private ISecurityConfiguration _securityConfiguration;

		[SetUp]
		public void SetUp()
		{
			// Arrange
			FluentSecurity.Reset();
			_configurationExpression = delegate { TestDataFactory.CreateValidConfigurationExpression(); };
			_securityConfiguration = MockRepository.GenerateMock<ISecurityConfiguration>();
			_securityConfiguration.Expect(x => x.Configure(_configurationExpression)).Return(_securityConfiguration).Repeat.Once();
			FluentSecurity.SetConfiguration(_securityConfiguration);
		}

		[Test]
		public void Should_call_configure_on_configuration()
		{
			// Act
			FluentSecurity.Configure(_configurationExpression);

			// Assert
			_securityConfiguration.AssertWasCalled(x => x.Configure(_configurationExpression));
		}
	}


	[TestFixture]
	[Category("FluentSecuritySpec")]
	public class When_I_check_what_I_have_on_fluent_security
	{
		private Action<ConfigurationExpression> _configurationExpression;
		private ISecurityConfiguration _securityConfiguration;

		[SetUp]
		public void SetUp()
		{
			// Arrange
			FluentSecurity.Reset();
			_configurationExpression = delegate { TestDataFactory.CreateValidConfigurationExpression(); };
			_securityConfiguration = MockRepository.GenerateMock<ISecurityConfiguration>();
			FluentSecurity.SetConfiguration(_securityConfiguration);
			FluentSecurity.Configure(_configurationExpression);
		}

		[Test]
		public void Should_call_WhatDoIHave_on_configuration()
		{
			// Act
			FluentSecurity.WhatDoIHave();

			// Assert
			_securityConfiguration.AssertWasCalled(x => x.WhatDoIHave());
		}
	}

	[TestFixture]
	[Category("FluentSecuritySpec")]
	public class When_I_configure_fluent_security_for_Blog_Index_and_Blog_AddPost
	{
		private IEnumerable<IPolicyContainer> _policyContainers;
		private DefaultPolicyManager _defaultPolicyManager;
		private IPolicyManager _fakePolicyManager;
		const string ControllerName = "Blog";
		const string IndexActionName = "Index";
		const string AddPostActionName = "AddPost";

		[SetUp]
		public void SetUp()
		{
			// Arrange
			_defaultPolicyManager = TestDataFactory.CreateValidPolicyManager();
			_fakePolicyManager = TestDataFactory.CreateFakePolicyManager();

			FluentSecurity.Reset();

			// Act
			FluentSecurity.Configure(configuration =>
			{
				configuration.GetAuthenticationStatusFrom(StaticHelper.IsAuthenticatedReturnsFalse);
				configuration.GetRolesFrom(StaticHelper.GetRolesExcludingOwner);

				configuration.SetCurrentPolicyManager(_defaultPolicyManager);
				configuration.For<BlogController>(x => x.Index()).DenyAnonymousAccess();

				configuration.SetCurrentPolicyManager(_fakePolicyManager);
				configuration.For<BlogController>(x => x.AddPost()).RequireRole(UserRole.Writer, UserRole.Publisher, UserRole.Owner);
			});

			_policyContainers = FluentSecurity.CurrentConfiguration.PolicyContainers;
		}

		[Test]
		public void Should_have_two_policycontainers()
		{
			Assert.That(_policyContainers.Count(), Is.EqualTo(2));
		}

		[Test]
		public void Should_have_policycontainer_for_Blog_Index()
		{
			var container = _policyContainers.GetContainerFor(ControllerName, IndexActionName);
			Assert.That(container.ControllerName, Is.EqualTo(ControllerName));
			Assert.That(container.ActionName, Is.EqualTo(IndexActionName));
			Assert.That(container.GetPolicies().Count(), Is.EqualTo(1));
			Assert.That(container.GetPolicies().First().GetType(), Is.EqualTo(typeof(DenyAnonymousAccessPolicy)));
			Assert.That(container.Manager, Is.EqualTo(_defaultPolicyManager));
		}

		[Test]
		public void Should_have_policycontainer_for_Blog_AddPost()
		{
			var container = _policyContainers.GetContainerFor(ControllerName, AddPostActionName);
			Assert.That(container.ControllerName, Is.EqualTo(ControllerName));
			Assert.That(container.ActionName, Is.EqualTo(AddPostActionName));
			Assert.That(container.GetPolicies().Count(), Is.EqualTo(1));
			Assert.That(container.GetPolicies().First().GetType(), Is.EqualTo(typeof(RequireRolePolicy)));
			Assert.That(container.Manager, Is.EqualTo(_fakePolicyManager));
		}
	}

	[TestFixture]
	[Category("FluentSecuritySpec")]
	public class When_adding_two_containers_with_the_same_controller_and_action_name
	{
		[Test]
		public void Should_have_1_policycontainer()
		{
			FluentSecurity.Reset();

			// Act
			FluentSecurity.Configure(configuration =>
			{
				configuration.GetAuthenticationStatusFrom(StaticHelper.IsAuthenticatedReturnsFalse);
				configuration.GetRolesFrom(StaticHelper.GetRolesExcludingOwner);
				configuration.For<BlogController>(x => x.Index());
				configuration.For<BlogController>(x => x.Index());
			});

			Assert.That(FluentSecurity.CurrentConfiguration.PolicyContainers.Count(), Is.EqualTo(1));
			Assert.That(FluentSecurity.CurrentConfiguration.PolicyContainers.First().ControllerName, Is.EqualTo("Blog"));
			Assert.That(FluentSecurity.CurrentConfiguration.PolicyContainers.First().ActionName, Is.EqualTo("Index"));
		}
	}

	[TestFixture]
	[Category("FluentSecuritySpec")]
	public class When_I_remove_policies_for_Blog_Index
	{
		private IEnumerable<IPolicyContainer> _policyContainers;
		const string ControllerName = "Blog";
		const string IndexActionName = "Index";

		[SetUp]
		public void SetUp()
		{
			// Arrange
			FluentSecurity.Reset();

			// Act
			FluentSecurity.Configure(configuration =>
			{
				configuration.GetAuthenticationStatusFrom(StaticHelper.IsAuthenticatedReturnsFalse);
				configuration.For<BlogController>(x => x.Index());
				configuration.For<BlogController>(x => x.AddPost());
				configuration.RemovePoliciesFor<BlogController>(x => x.Index());
			});

			_policyContainers = FluentSecurity.CurrentConfiguration.PolicyContainers;
		}

		[Test]
		public void Should_have_1_policycontainer()
		{
			// Assert
			Assert.That(_policyContainers.Count(), Is.EqualTo(1));
		}

		[Test]
		public void Should_not_have_policycontainer_for_Blog_Index()
		{
			// Assert
			var container = _policyContainers.GetContainerFor(ControllerName, IndexActionName);
			Assert.That(container, Is.Null);
		}
	}
}